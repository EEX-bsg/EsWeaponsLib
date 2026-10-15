using EWL.Api;
using Modding.Modules;
using UnityEngine;

namespace EwlSampleCSharp
{
    // EWL の Weapon を自分のモジュールで作って駆動する例(形態 C)。
    // 弾・命中・爆発・エフェクト・MP 同期は EWL が行う。ここで決めるのは「どんな武装か」と「いつ撃つか」だけ。
    public class SampleCannonBehaviour : BlockModuleBehaviour<SampleCannonModule>
    {
        private MKey _fireKey;
        private MSlider _powerSlider;
        private Weapon _weapon;

        public override void SafeAwake()
        {
            _fireKey = GetKey(Module.FireKey);
            if (Module.PowerSlider != null) _powerSlider = GetSlider(Module.PowerSlider);
        }

        public override void OnSimulateStart()
        {
            var spec = new WeaponSpec
            {
                MuzzlePosition = new Vector3(0f, 0f, 1.3f),
                MuzzleVelocity = _powerSlider != null ? _powerSlider.Value : 150f,
                FireInterval = Module.Interval,
            };
            spec.Projectile.Mass = 0.5f;
            spec.Projectile.Lifetime = 8f;
            spec.Projectile.BlockDamage = 1f;
            spec.Projectile.Explosion.Radius = Module.ExplosionRadius;
            spec.Projectile.Explosion.Force = 2000f;

            // 全ピアで作る(クライアントの見た目の弾も同じ定義から出るため)。破棄は OnSimulateStop で対にする
            _weapon = Weapon.Create(BlockBehaviour, spec);
            _weapon.Hit += OnHit;
        }

        public override void OnSimulateStop()
        {
            if (_weapon == null) return;
            _weapon.Hit -= OnHit;
            _weapon.Destroy();
            _weapon = null;
        }

        // 実行権威のある側で呼ばれる。実キーとエミュレーション(ロジックブロック等)の両方を見る
        public override void SimulateUpdateHost()
        {
            if (_weapon == null) return;
            if (!_fireKey.IsHeld && !_fireKey.EmulationHeld(true)) return;
            // Spec は発射のたびに読まれるので、撃つ前に書き換えればその弾から効く(シミュ中にスライダーが変わっても追従する)
            if (_powerSlider != null) _weapon.Spec.MuzzleVelocity = _powerSlider.Value;
            _weapon.TryFire();
        }

        // 実行権威のある側でだけ発火する。命中のたびに呼ばれるので重い処理をしない
        private void OnHit(ProjectileHit hit)
        {
            if (hit.Kind == HitKind.Direct && hit.Block != null)
                Debug.Log("[EwlSampleCSharp] Direct hit on " + hit.Block.name);
        }
    }
}
