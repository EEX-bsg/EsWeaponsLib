using EWL.Api;
using UnityEngine;

namespace EWL.Besiege
{
    // バニラの矢(ProjectileScript)の監視。本体が命中を処理すると hasAttached が立つので、その変化を直撃として通知する。
    // 衝突コールバックは同じ物体の部品の並び順に呼ばれ、本体の部品が先(この部品は後から付ける)なので、
    // 自分のコールバックで変化を見つけたら、その接触が本体の処理した命中。
    internal sealed class VanillaArrowWatch : MonoBehaviour
    {
        private ProjectileScript _arrow;
        private bool _wasAttached; // 飛んでいる状態を FixedUpdate で一度見るまでは true(弾倉の準備中の変化を数えない)

        private void Awake() { _arrow = GetComponent<ProjectileScript>(); }

        private void OnEnable() { _wasAttached = true; }

        private void FixedUpdate()
        {
            CheckHit(null, null);
            if (_arrow != null) _wasAttached = _arrow.hasAttached; // 弾かれる(Deflect)と戻る
        }

        private void OnCollisionEnter(Collision collision)
        {
            CheckHit(collision.collider, collision);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.isTrigger) CheckHit(other, null);
        }

        // 本体が命中の処理の中で矢を無効にした場合、自分の衝突コールバックは届かない
        private void OnDisable() { CheckHit(null, null); }

        // contact: 本体が直前に処理した接触(分からなければ null)。collision: 接触点を取る衝突(無ければ矢の位置)
        private void CheckHit(Collider contact, Collision collision)
        {
            if (_arrow == null || _wasAttached || !_arrow.hasAttached || !Simulation.SimulatesVanillaProjectiles) return;
            _wasAttached = true;
            var shooter = AttackerTags.OfVanillaProjectile(gameObject);
            if (!VanillaProjectiles.Listening || (StatMaster.isMP && !StatMaster.isLocalSim && shooter == null)) return; // NPC の矢
            // 接触が取れなかったときは、刺さった先(矢の親)から当たったブロックを取る
            var block = contact != null ? Damage.BlockOf(contact)
                : transform.parent != null ? transform.parent.GetComponentInParent<BlockBehaviour>() : null;
            var contacts = collision != null ? collision.contacts : null;
            var point = contacts != null && contacts.Length > 0 ? contacts[0].point : transform.position;
            VanillaProjectiles.RaiseHit(HitSource.VanillaCrossbow, shooter, block, contact, point, HitKind.Direct);
        }
    }
}
