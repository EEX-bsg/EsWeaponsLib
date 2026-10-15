using EWL.Api;
using UnityEngine;

namespace EWL.Besiege
{
    // バニラの FuelCannon の火球(FireBallDamage)の監視。着弾すると本体が剛体を kinematic にして止める
    // (MP ホスト・ローカルシミュはプールへ戻して無効化、SP はコンポーネントだけ無効化)ので、それを着弾として通知する。
    // 当たった相手は本体の private な処理の中にしか無いので、直前の位置から本体と同じ球キャストをやり直して決める(近似)。
    internal sealed class VanillaFireballWatch : MonoBehaviour
    {
        private const int LoadingLayer = 8; // 発射準備中に本体が一時的に置くレイヤー
        private static readonly RaycastHit[] CastHits = new RaycastHit[32];
        private static readonly Collider[] OverlapHits = new Collider[32];

        private FireBallDamage _fireball;
        private Rigidbody _rb;
        private bool _flying;
        private Vector3 _lastPosition;
        private Vector3 _lastVelocity;

        private void Awake()
        {
            _fireball = GetComponent<FireBallDamage>();
            _rb = GetComponent<Rigidbody>();
        }

        private void OnEnable() { _flying = false; }

        private void FixedUpdate()
        {
            if (_fireball == null || _rb == null) return;
            if (_flying && _rb.isKinematic && !_fireball.enabled)
            {
                Impact();
                return;
            }
            _flying = _fireball.enabled && !_rb.isKinematic && gameObject.layer != LoadingLayer;
            _lastPosition = _rb.position;
            _lastVelocity = _rb.velocity;
        }

        private void OnDisable()
        {
            // 着弾以外(寿命・シミュ終了)の無効化では kinematic にならない
            if (_flying && _rb != null && _rb.isKinematic) Impact();
            _flying = false;
        }

        private void Impact()
        {
            _flying = false;
            if (!Simulation.SimulatesVanillaProjectiles || !VanillaProjectiles.Listening) return;
            Collider hit;
            Vector3 point;
            FindHit(out hit, out point);
            VanillaProjectiles.RaiseHit(HitSource.VanillaFuelCannon, AttackerTags.OfVanillaProjectile(gameObject),
                Damage.BlockOf(hit), hit, point, HitKind.Direct);
        }

        // 本体と同じく、経路上で最も近いもの。経路は本体の1ステップ分を含むよう2ステップ分とる
        private void FindHit(out Collider hit, out Vector3 point)
        {
            hit = null;
            point = transform.position;
            float radius = _fireball.radius * transform.localScale.x;
            var path = _lastVelocity * Time.fixedDeltaTime * 2f;
            float length = path.magnitude;
            float nearest = float.MaxValue;
            if (length > 0f)
            {
                int count = Physics.SphereCastNonAlloc(_lastPosition, radius, path / length, CastHits, length,
                    _fireball.burnMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    if (CastHits[i].distance >= nearest) continue;
                    nearest = CastHits[i].distance;
                    hit = CastHits[i].collider;
                    // 始点で重なっていたものは距離0・接触点がゼロで返る
                    point = CastHits[i].point != Vector3.zero ? CastHits[i].point : transform.position;
                }
            }
            if (hit != null) return;
            // 経路の始点で既に重なっていた(球キャストは始点で重なるものを返さない)
            int overlapCount = Physics.OverlapSphereNonAlloc(transform.position, radius, OverlapHits, _fireball.burnMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlapCount; i++)
            {
                float distance = (OverlapHits[i].ClosestPointOnBounds(transform.position) - transform.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                hit = OverlapHits[i];
            }
        }
    }
}
