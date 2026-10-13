using System.Collections.Generic;
using EWL.Api;
using UnityEngine;

namespace EWL.Besiege
{
    // バニラのボムに付けて「誰が起爆させたか」を記録する。シミュ権威側のシミュ用ボムにだけ付く。
    // 位置では関連付けない。ボムの爆発(Explodey)は「hasExploded を立てる → 周りへ OnExplode → 最後に非表示」を同期で行うので、
    // 爆発済みでまだ有効なボム = 爆発処理の最中。あるボムが爆発し終えた時点で他に処理中のボムがあれば、それは同じ連鎖(呼び出しの入れ子)。
    // 連鎖の根本は最後に非表示になる(最も外側の)ボムで、その起爆のきっかけが連鎖全体のタグになる。
    // グレネード・ロケット・燃料樽は爆発が非同期でこの関係が成り立たないので、その爆風で起爆したボムは不明になる。
    internal sealed class ExplosiveTag : MonoBehaviour
    {
        private sealed class DetonationChain
        {
            internal Attacker Attacker; // EWL が処理中に起きた連鎖ならその攻撃者
            internal ExplosiveTag Root;

            internal Attacker Resolve()
            {
                if (Attacker != null) return Attacker;
                return Root != null ? Root.OwnCause() : null;
            }
        }

        private static readonly List<ExplosiveTag> Enabled = new List<ExplosiveTag>();
        private static readonly List<ExplosiveTag> Alive = new List<ExplosiveTag>(); // 診断用(シミュ終了で破棄されるまで)

        private ExplodeOnCollideBlock _bomb;
        private DetonationChain _chain;
        private float _detonatedAt = -1f;
        // 爆発と同じ瞬間に起きたかを比べる記録(同じ接触を両側のコールバックで受けるため、時刻は一致する)
        private Attacker _directHit;
        private float _directHitAt = -1f;
        private float _blockCollisionAt = -1f;

        /// <summary>Events.OnBlockInit から。シミュ権威側のシミュ用ボムに付ける(ビルド用ブロックに付けるとクローンへ複製されるので付けない)。</summary>
        internal static void OnBlockInit(Modding.Blocks.Block block)
        {
            // 全ブロック(他 Mod を含む)の初期化で呼ばれるので、例外を他の購読者へ漏らさない
            try
            {
                var bomb = block.InternalObject as ExplodeOnCollideBlock;
                if (bomb == null || bomb.isBuildBlock || !Simulation.HasAuthority(bomb) || bomb.GetComponent<ExplosiveTag>() != null) return;
                bomb.gameObject.AddComponent<ExplosiveTag>()._bomb = bomb;
            }
            catch (System.Exception ex) { Debug.LogError("[EWL] ExplosiveTag.OnBlockInit failed: " + ex); }
        }

        internal static Attacker DetonatorOf(BlockBehaviour block)
        {
            var tag = block.GetComponent<ExplosiveTag>();
            return tag != null && tag._chain != null ? tag._chain.Resolve() : null;
        }

        /// <summary>EWL の弾の直撃。ボムが自分の衝突処理で先に爆発していても、同じ瞬間なら根本のきっかけになる。</summary>
        internal static void NoteDirectHit(Rigidbody body, Attacker attacker)
        {
            var tag = body.GetComponent<ExplosiveTag>();
            if (tag == null || attacker == null) return;
            tag._directHit = attacker;
            tag._directHitAt = Time.time;
        }

        /// <summary>コンソールコマンド ewl_detonations: このシミュで爆発したボムと起爆させた人を出す(実行権威側の記録)。</summary>
        internal static void LogDetonations(string[] args)
        {
            int count = 0;
            foreach (var tag in Alive)
            {
                if (tag._chain == null) continue;
                count++;
                Modding.ModConsole.Log("[EWL] " + tag.Describe());
            }
            Modding.ModConsole.Log("[EWL] detonated bombs: " + count);
        }

        /// <summary>爆発処理の最中(有効なタグだけが対象なので、爆発済み = まだ非表示になっていない)。</summary>
        private bool IsDetonating { get { return _bomb != null && _bomb.hasExploded; } }

        private void Awake() { Alive.Add(this); }

        private void OnDestroy() { Alive.Remove(this); }

        private void OnEnable() { Enabled.Add(this); }

        private void OnDisable()
        {
            Enabled.Remove(this);
            // 内側で誘爆したボムが先に連鎖へ入れていることがあるので、連鎖の有無ではなく自分の爆発を記録したかで判定する
            if (_detonatedAt < 0f && IsDetonating) Detonated();
        }

        private void OnCollisionEnter(Collision collision) { NoteContact(collision.collider); }

        private void OnTriggerEnter(Collider other) { NoteContact(other); }

        private void NoteContact(Collider other)
        {
            var shooter = AttackerTags.OfProjectile(other) ?? FiredRocketOwner(other);
            if (shooter != null)
            {
                _directHit = shooter;
                _directHitAt = Time.time;
            }
            else if (other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<BlockBehaviour>() != null)
            {
                _blockCollisionAt = Time.time; // 地形・機体のブロック(落下・体当たり・投下)
            }
            // それ以外(他 Mod の弾・エンティティ等)は分からないので記録しない
        }

        // 発射済みのバニラロケットは武器として飛んでいるので、ブロック衝突ではなく持ち主の攻撃とみなす
        private static Attacker FiredRocketOwner(Collider other)
        {
            var rocket = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<TimedRocket>() : null;
            return rocket != null && rocket.hasFired ? AttackerTags.OfBlock(rocket) : null;
        }

        private void Detonated()
        {
            _detonatedAt = Time.time;
            var chain = _chain;
            bool outermost = true;
            foreach (var other in Enabled)
            {
                if (!other.IsDetonating) continue;
                outermost = false;
                if (chain == null) chain = other._chain;
            }
            if (chain == null) chain = new DetonationChain();
            foreach (var other in Enabled)
                if (other.IsDetonating && other._chain == null) other._chain = chain;
            _chain = chain;
            if (chain.Attacker == null) chain.Attacker = AttackerTags.Current;
            if (outermost) chain.Root = this;
        }

        // 連鎖の根本になったときの起爆のきっかけ。読むときに決める(弾側の衝突処理が後から届くことがあるため)。
        private Attacker OwnCause()
        {
            if (_directHitAt == _detonatedAt && _directHit != null) return _directHit;
            if (_blockCollisionAt == _detonatedAt) return AttackerTags.OfBlock(_bomb); // 自爆(落下・体当たり・投下はボムの持ち主)
            return null;
        }

        private string OwnCauseName()
        {
            if (_directHitAt == _detonatedAt && _directHit != null) return "projectile hit";
            if (_blockCollisionAt == _detonatedAt) return "block/terrain collision";
            return "unknown trigger";
        }

        private string Describe()
        {
            var detonator = _chain.Resolve();
            string via;
            if (_chain.Attacker != null) via = "EWL attack in progress";
            else if (_chain.Root == null) via = "chain without root";
            else if (_chain.Root == this) via = OwnCauseName();
            else via = "chain from bomb #" + _chain.Root._bomb.BuildIndex + " (" + _chain.Root.OwnCauseName() + ")";
            return "bomb #" + _bomb.BuildIndex + " of player " + Simulation.PlayerIdOf(_bomb)
                + " detonated by " + (detonator != null ? "player " + detonator.PlayerId : "unknown") + " via " + via;
        }
    }
}
