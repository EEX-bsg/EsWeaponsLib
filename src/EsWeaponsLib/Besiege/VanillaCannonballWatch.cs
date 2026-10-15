using EWL.Api;
using UnityEngine;

namespace EWL.Besiege
{
    // バニラの砲弾(CannonBallDamage)の監視。本体と同じ条件でダメージが入った衝突を直撃として通知する。
    // 爆発する砲弾は直撃のダメージが無く、爆風(VanillaBlastWatch)で通知する。
    internal sealed class VanillaCannonballWatch : MonoBehaviour
    {
        private CannonBallDamage _cannonball;
        private Rigidbody _rb;
        private float _lastSqrVelocity; // 本体の判定に使う値(private なので同じ式で計算し直す)
        private Attacker _explodingShooter; // 爆発が決まった弾の撃ち手(MP ホストで爆風と結ぶ)
        private bool _explodes;

        private void Awake()
        {
            _cannonball = GetComponent<CannonBallDamage>();
            _rb = GetComponent<Rigidbody>();
        }

        private void OnEnable() { _explodes = false; }

        private void FixedUpdate()
        {
            if (_rb != null) _lastSqrVelocity = (_rb.velocity * _rb.mass * 0.5f).sqrMagnitude;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_cannonball == null || !_cannonball.SimPhysics || !Simulation.SimulatesVanillaProjectiles) return;
            var other = collision.collider;
            var machine = VanillaProjectiles.ShooterMachine(gameObject);
            // 本体は自分の機体への衝突を無視する
            if (!_cannonball.alwaysExplode && _cannonball.playerProjectile && machine != null && other.transform.IsChildOf(machine.transform)) return;
            if ((_cannonball.alwaysExplode || (machine != null && machine.ExplodingCannonballs)) && !_cannonball.isChain)
            {
                _explodes = true;
                _explodingShooter = AttackerTags.OfVanillaProjectile(gameObject);
                return;
            }
            if (other.attachedRigidbody == null || _lastSqrVelocity < _cannonball.velocitySqrThreshold / 2f || !VanillaProjectiles.Listening) return;
            if (AttackerTags.OfProjectile(other) != null) return; // 弾どうし(本体は何も起こさない)
            var contacts = collision.contacts;
            var point = contacts.Length > 0 ? contacts[0].point : transform.position;
            VanillaProjectiles.RaiseHit(HitSource.VanillaCannon, AttackerTags.OfVanillaProjectile(gameObject), Damage.BlockOf(other), other, point, HitKind.Direct);
        }

        // MP ホストでは爆発の中で爆風を生成した直後に弾をプールへ戻す(同じ呼び出しの中)ので、ここで爆風と結ぶ
        private void OnDisable()
        {
            if (!_explodes) return;
            _explodes = false;
            VanillaBlastWatch.LinkJustCreated(_explodingShooter);
        }
    }
}
