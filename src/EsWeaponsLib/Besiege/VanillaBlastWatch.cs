using System.Collections.Generic;
using EWL.Api;
using UnityEngine;

namespace EWL.Besiege
{
    // バニラの爆発する砲弾の爆風(CannonBallExplosion)の監視。本体は爆風を弾の爆発の中で Instantiate し(Awake は同期)、
    // 次の Start の中で力と OnExplode を同期で与え、最後に hasExploded を立てる。Start の冒頭で power を2倍にするので、
    // 「power が生成時と違い、hasExploded がまだ」= いま爆風処理の最中(位置では判定しない)。その中で起爆したボムはこの爆風の撃ち手のもの。
    // 撃ち手: SP・ローカルシミュは自分。MP ホストは爆発の直後に同じ呼び出しの中で弾がプールへ戻る(OnDisable)ので、そこで結ぶ。
    internal sealed class VanillaBlastWatch : MonoBehaviour
    {
        private static readonly List<VanillaBlastWatch> Alive = new List<VanillaBlastWatch>();
        private static VanillaBlastWatch _justCreated;

        private CannonBallExplosion _blast;
        private float _initialPower;
        private bool _armed;
        private bool _linked;
        private Attacker _shooter;
        private HashSet<Rigidbody> _bodies; // 力を受けた剛体(命中として通知する。聞き手がいるときだけ)
        private List<Rigidbody> _detonated; // 爆風で起爆して消えたボム(範囲を数えた時点で消えていることがある)

        /// <summary>MP ホストで、爆発した弾が無効になったとき。直前に生成された爆風をその弾のものとする。</summary>
        internal static void LinkJustCreated(Attacker shooter)
        {
            var blast = _justCreated;
            _justCreated = null;
            if (blast == null || blast._linked) return;
            blast._linked = true;
            blast._shooter = shooter;
        }

        /// <summary>
        /// ボムが爆発し終えたとき(ExplosiveTag から)。バニラの爆風処理の最中ならその撃ち手を返し、命中として数える。
        /// 処理中の爆風が見つからない・複数で撃ち手が食い違うときは null。
        /// </summary>
        internal static Attacker NoteDetonation(Rigidbody bomb)
        {
            // ボムの起爆の記録(ExplosiveTag)の途中で呼ばれるので、例外をそちらへ漏らさない
            try { return FindExploding(bomb); }
            catch (System.Exception ex)
            {
                Debug.LogError("[EWL] VanillaBlastWatch.NoteDetonation failed: " + ex);
                return null;
            }
        }

        private static Attacker FindExploding(Rigidbody bomb)
        {
            VanillaBlastWatch found = null;
            foreach (var watch in Alive)
            {
                if (!watch.IsExploding) continue;
                if (found != null && !SameAttacker(found._shooter, watch._shooter)) return null;
                found = watch;
            }
            if (found == null) return null;
            if (bomb != null)
            {
                if (found._detonated == null) found._detonated = new List<Rigidbody>();
                found._detonated.Add(bomb);
            }
            return found._shooter;
        }

        private bool IsExploding { get { return _armed && _blast.power != _initialPower && !_blast.hasExploded; } }

        private void Awake()
        {
            _blast = GetComponent<CannonBallExplosion>();
            // 見本に付けたときの Awake と、本体の命中を処理しない側は何もしない
            if (_blast == null || VanillaProjectiles.Patching || !Simulation.SimulatesVanillaProjectiles) return;
            _armed = true;
            _initialPower = _blast.power;
            Alive.Add(this);
            if (!StatMaster.isMP || StatMaster.isLocalSim)
            {
                _linked = true;
                _shooter = AttackerTags.LocalPlayer();
            }
            else _justCreated = this;
        }

        // 同じ物体の Start は部品の並び順で、本体の爆風(先に付いている)の後になる。力はまだ物理ステップで反映されていないので、
        // ここで範囲を数えれば本体と同じ剛体になる(爆風の中で起爆して消えたボムは NoteDetonation で足す)。
        private void Start()
        {
            if (_justCreated == this) _justCreated = null; // 結ぶ機会は生成と同じ呼び出しの中だけ
            if (!_armed || !VanillaProjectiles.Listening) return;
            // 本体の処理がまだなら、本体と同じく大きさを掛けた半径を使う
            float radius = _blast.hasExploded ? _blast.radius : _blast.radius * transform.localScale.x / _blast.originalScale.x;
            _bodies = new HashSet<Rigidbody>();
            foreach (var collider in Physics.OverlapSphere(transform.position, radius))
            {
                var rb = collider.attachedRigidbody;
                if (Damage.IsExplosionTarget(rb) && AttackerTags.OfProjectile(collider) == null) _bodies.Add(rb); // 爆発した弾自身などは除く
            }
        }

        private void LateUpdate()
        {
            if (!_armed || !_blast.hasExploded) return;
            _armed = false;
            Alive.Remove(this);
            if (_bodies != null) Report();
        }

        private void OnDestroy()
        {
            Alive.Remove(this);
            if (_justCreated == this) _justCreated = null;
        }

        private void Report()
        {
            if (_detonated != null) _bodies.UnionWith(_detonated);
            var pos = transform.position;
            foreach (var rb in _bodies)
            {
                if (rb == null) continue;
                VanillaProjectiles.RaiseHit(HitSource.VanillaCannon, _shooter, rb.GetComponent<BlockBehaviour>(), null, pos, HitKind.Explosion);
            }
        }

        private static bool SameAttacker(Attacker first, Attacker second)
        {
            if (first == null || second == null) return first == second;
            return first.PlayerId == second.PlayerId && Equals(first.Player, second.Player);
        }
    }
}
