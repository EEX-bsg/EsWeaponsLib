using System.Collections.Generic;
using EWL.Api;
using Modding;
using Modding.Modules;
using Modding.Serialization;
using UnityEngine;

namespace EWL.Modules
{
    // EwlWeapon モジュール = 公開 API(EWL.Api)の標準実装。EWL の内部 namespace は使わない。
    // 仕事: XML → WeaponSpec 変換、ブロック定義 Mod のリソース解決、キー入力 → Weapon.TryFire。
    public class EwlWeaponBehaviour : BlockModuleBehaviour<EwlWeaponModule>
    {
        private const float PowerToForce = 100f; // Power 1 あたりの力 [N](公式 Shooting モジュールと同じ)

        private MKey _fireKey;
        private MToggle _holdToggle;
        private MKey _reloadKey;
        private MToggle _autoReloadToggle;
        private MSlider _fireRateSlider;
        private MSlider _fireDelaySlider;
        private MSlider _timeFuseSlider;
        private MSlider _magazineCapacitySlider;
        private MSlider _powerSlider;
        private MSlider _boosterPowerSlider;
        private MSlider _thrustTimeSlider;
        private Weapon _weapon;
        private bool _autoFire;
        private bool _fireHeldBefore;
        private bool _reloadHeldBefore;
        private ModAssetBundle _effectBundle;

        public override void SafeAwake()
        {
            _fireKey = GetKey(Module.FireKey);
            if (Module.HoldToFireToggle != null) _holdToggle = GetToggle(Module.HoldToFireToggle);
            if (Module.ReloadKey != null) _reloadKey = GetKey(Module.ReloadKey);
            if (Module.AutoReloadToggle != null) _autoReloadToggle = GetToggle(Module.AutoReloadToggle);
            if (Module.FireRateSlider != null) _fireRateSlider = GetSlider(Module.FireRateSlider);
            if (Module.FireDelaySlider != null) _fireDelaySlider = GetSlider(Module.FireDelaySlider);
            if (Module.TimeFuseSlider != null) _timeFuseSlider = GetSlider(Module.TimeFuseSlider);
            if (Module.MagazineCapacitySlider != null) _magazineCapacitySlider = GetSlider(Module.MagazineCapacitySlider);
            if (Module.PowerSlider != null) _powerSlider = GetSlider(Module.PowerSlider);
            if (Module.BoosterPowerSlider != null) _boosterPowerSlider = GetSlider(Module.BoosterPowerSlider);
            if (Module.ThrustTimeSlider != null) _thrustTimeSlider = GetSlider(Module.ThrustTimeSlider);
        }

        // ビルド中のブロック(読込・配置)だけで呼ばれる。弾が無い武装は装填中の弾を出さない(シミュ中と同じ)。
        public override void OnBlockPlaced()
        {
            if (!Module.ShowLoadedProjectile || Module.TotalAmmo == 0) return;
            Weapon.CreateLoadedProjectilePreview(BlockBehaviour, BuildSpec());
        }

        public override void OnSimulateStart()
        {
            _autoFire = _fireHeldBefore = _reloadHeldBefore = false;
            _weapon = Weapon.Create(BlockBehaviour, BuildSpec());
            _effectBundle = ResolveBundle(Module.AssetBundle);
            // 本体はバンドルを非同期で読む。未読込なら読込完了で差し込む(読込済みで購読すると本体が全アセットを読み直すので、そのときは購読しない)
            if (_effectBundle != null && !_effectBundle.Loaded) _effectBundle.OnLoad += SetEffectPrefabs;
            else SetEffectPrefabs();
        }

        public override void OnSimulateStop()
        {
            if (_effectBundle != null) _effectBundle.OnLoad -= SetEffectPrefabs;
            _effectBundle = null;
            if (_weapon != null) _weapon.Destroy();
            _weapon = null;
        }

        public override void SimulateUpdateHost()
        {
            if (_weapon == null) return;
            bool hold = _holdToggle != null ? _holdToggle.IsActive : Module.HoldToFire;
            bool fireHeld = IsHeld(_fireKey);
            bool firePressed = fireHeld && !_fireHeldBefore;
            _fireHeldBefore = fireHeld;
            bool fire;
            if (Module.FireMode == FireMode.SemiAuto) fire = firePressed;
            else
            {
                if (!hold && firePressed) _autoFire = !_autoFire;
                fire = hold ? fireHeld : _autoFire;
            }
            if (fire)
            {
                ApplySliders(_weapon.Spec);
                if (_fireRateSlider == null || _fireRateSlider.Value > 0f) _weapon.TryFire();
            }
            if (_reloadKey != null)
            {
                bool reloadHeld = IsHeld(_reloadKey);
                if (reloadHeld && !_reloadHeldBefore) _weapon.TryReload();
                _reloadHeldBefore = reloadHeld;
            }
        }

        // 押した瞬間は IsHeld の変化で取る。MP ホストではクライアントの IsPressed が1フレームしか立たず取りこぼすことがある。
        // 実キーとエミュレーション(ロジックブロック等)は別経路。
        private static bool IsHeld(MKey key)
        {
            return key.IsHeld || key.EmulationHeld(true);
        }

        private WeaponSpec BuildSpec()
        {
            var module = Module;
            var spec = new WeaponSpec
            {
                MuzzlePosition = module.MuzzlePosition,
                MuzzleRotation = module.MuzzleRotation,
                MuzzleVelocity = module.MuzzleVelocity,
                EjectVelocity = module.EjectVelocity,
                FireInterval = module.FireInterval,
                FireDelay = module.FireDelay,
                FireDelayRandomExtra = module.FireDelayRandomExtra,
                Spread = module.Spread,
                RecoilMultiplier = module.RecoilMultiplier,
                BurstCount = module.BurstCount,
                BurstInterval = module.BurstInterval,
                StartDelay = module.StartDelay,
                KeepFireIntervalOnRespawn = module.KeepFireIntervalOnRespawn,
                TotalAmmo = module.TotalAmmo,
                // 弾倉は武装の生成時に詰めるので、容量はシミュ開始時だけ読む(発射ごとの ApplySliders には入れない)
                MagazineCapacity = _magazineCapacitySlider != null
                    ? Mathf.RoundToInt(_magazineCapacitySlider.Value)
                    : module.MagazineCapacity,
                ReloadTime = module.ReloadTime,
                AutoReload = _autoReloadToggle != null ? _autoReloadToggle.IsActive : module.AutoReload,
                ShowLoadedProjectile = module.ShowLoadedProjectile,
                MuzzleFlashPosition = module.MuzzleFlashPosition,
                MuzzleFlashRotation = module.MuzzleFlashRotation,
                MuzzleFlashScale = module.MuzzleFlashScale,
            };
            AddClips(module.FireSounds, spec.FireSounds, "FireSounds");
            AddClips(module.ReloadSounds, spec.ReloadSounds, "ReloadSounds");

            var projectileXml = module.Projectile;
            var projectile = spec.Projectile;
            projectile.Mass = projectileXml.Mass;
            projectile.Drag = projectileXml.Drag;
            projectile.AngularDrag = projectileXml.AngularDrag;
            projectile.AerodynamicStability = projectileXml.AerodynamicStability;
            projectile.IgnoreGravity = projectileXml.IgnoreGravity;
            projectile.CollisionMode = projectileXml.CollisionMode;
            projectile.Lifetime = projectileXml.Lifetime;
            projectile.ColliderEnableDelay = projectileXml.ColliderEnableDelay;
            if (projectileXml.Colliders != null)
                foreach (var collider in projectileXml.Colliders) projectile.Colliders.Add(ToShape(collider));
            projectile.Friction = projectileXml.Friction;
            projectile.Bounciness = projectileXml.Bounciness;
            projectile.FrictionCombine = projectileXml.FrictionCombine;
            projectile.BounceCombine = projectileXml.BounceCombine;
            projectile.PoolSize = projectileXml.PoolSize;
            projectile.PoolKey = PoolKeyFor(module);
            if (projectileXml.Mesh != null)
            {
                var mesh = Resolve<ModMesh>(projectileXml.Mesh, "Projectile/Mesh");
                projectile.Mesh = mesh != null ? mesh.Mesh : null;
                projectile.MeshPosition = projectileXml.Mesh.Position;
                projectile.MeshRotation = projectileXml.Mesh.Rotation;
                projectile.MeshScale = projectileXml.Mesh.Scale;
            }
            var texture = Resolve<ModTexture>(projectileXml.Texture, "Projectile/Texture");
            projectile.Texture = texture != null ? texture.Texture : null;
            projectile.ImpactForce = projectileXml.ImpactForce;
            projectile.BlockDamage = projectileXml.BlockDamage;
            projectile.EntityDamage = projectileXml.EntityDamage;
            projectile.AttachOnHit = projectileXml.AttachOnHit;
            projectile.Penetration = projectileXml.Penetration;
            projectile.PenetrationMode = projectileXml.PenetrationMode;
            projectile.DespawnOnHit = projectileXml.DespawnOnHit;
            projectile.TimeFuse = projectileXml.TimeFuse;
            projectile.TimeFuseRandomExtra = projectileXml.TimeFuseRandomExtra;
            projectile.DetonationDelay = projectileXml.DetonationDelay;
            projectile.DetonationDelayRandomExtra = projectileXml.DetonationDelayRandomExtra;

            var explosionXml = projectileXml.Explosion;
            var explosion = projectile.Explosion;
            explosion.Radius = explosionXml.Radius;
            explosion.Force = explosionXml.Force;
            explosion.UpwardsModifier = explosionXml.UpwardsModifier;
            explosion.OnlyWithExplodingCannonballs = explosionXml.OnlyWithExplodingCannonballs;
            explosion.ForwardOffset = explosionXml.ForwardOffset;
            explosion.FacesHitNormal = explosionXml.FacesHitNormal;
            AddClips(explosionXml.Sounds, explosion.Sounds, "Projectile/Explosion/Sounds");

            var boosterXml = projectileXml.Booster;
            projectile.Booster.Thrust = boosterXml.Thrust;
            projectile.Booster.ThrustTime = boosterXml.ThrustTime;
            projectile.Booster.Stability = boosterXml.Stability;
            projectile.Booster.Lift = boosterXml.Lift;
            projectile.Booster.MaxSpeed = boosterXml.MaxSpeed;
            projectile.Guidance.Gain = projectileXml.Guidance.Gain;
            projectile.Guidance.ReleaseAngle = projectileXml.Guidance.ReleaseAngle;
            projectile.Guidance.PredictionIterations = projectileXml.Guidance.PredictionIterations;

            ApplySliders(spec);
            return spec;
        }

        // スライダーはシミュ中に他のモジュールが動かすことがある(キーで信管時間を変える等)ので、発射ごとに読み直す。
        private void ApplySliders(WeaponSpec spec)
        {
            if (_fireRateSlider != null && _fireRateSlider.Value > 0f) spec.FireInterval = 1f / _fireRateSlider.Value;
            if (_fireDelaySlider != null) spec.FireDelay = Module.FireDelay + _fireDelaySlider.Value;
            if (_timeFuseSlider != null) spec.Projectile.TimeFuse = _timeFuseSlider.Value;
            // 公式 Shooting モジュールは 100 × Power の力を1物理ステップかけて撃つ。その速度変化をそのまま初速にする
            var projectile = spec.Projectile;
            if (_powerSlider != null && projectile.Mass > 0f)
                spec.MuzzleVelocity = PowerToForce * _powerSlider.Value * Time.fixedDeltaTime / projectile.Mass;
            if (_boosterPowerSlider != null) projectile.Booster.Thrust = PowerToForce * _boosterPowerSlider.Value;
            if (_thrustTimeSlider != null) projectile.Booster.ThrustTime = _thrustTimeSlider.Value;
        }

        // Spec は発射のたびに読まれるので、シミュ中に差し込んでも次の弾から反映される。
        private void SetEffectPrefabs()
        {
            if (this == null || _weapon == null) return; // OnSimulateStop を経ずに破棄された後の OnLoad
            var module = Module;
            var spec = _weapon.Spec;
            spec.MuzzleFlash = Prefab(module.MuzzleFlash);
            spec.Projectile.Trail = Prefab(module.Projectile.Trail);
            spec.Projectile.AttachedEffect = Prefab(module.Projectile.AttachedEffect);
            spec.Projectile.Explosion.Effect = Prefab(module.Projectile.Explosion.Effect);
        }

        // モジュールのインスタンスはブロック定義ごとに1つなので、同じブロック種別の武装は弾のプールを共有する。
        // キーは Projectile 要素ごと: XML の再読込で要素が差し替わると別のプールになり、弾の形(プールごとに固定)も作り直される。
        // 古いプールはシーンが変わるまで残る(再読込は Debug の Mod だけ)。
        private static readonly Dictionary<ProjectileXml, string> PoolKeys = new Dictionary<ProjectileXml, string>();

        private static string PoolKeyFor(EwlWeaponModule module)
        {
            string key;
            if (!PoolKeys.TryGetValue(module.Projectile, out key)) PoolKeys[module.Projectile] = key = "EwlWeapon#" + PoolKeys.Count;
            return key;
        }

        private static ColliderShape ToShape(ModCollider collider)
        {
            var shape = new ColliderShape { Position = collider.Position };
            var capsule = collider as CapsuleModCollider;
            var box = collider as BoxModCollider;
            var sphere = collider as SphereModCollider;
            if (capsule != null)
            {
                shape.Kind = ColliderKind.Capsule;
                shape.Rotation = capsule.Rotation;
                shape.Radius = capsule.Radius;
                shape.Height = capsule.Height;
                shape.Direction = (int)capsule.Dir;
            }
            else if (box != null)
            {
                shape.Kind = ColliderKind.Box;
                shape.Rotation = box.Rotation;
                shape.Size = box.Scale;
            }
            else if (sphere != null)
            {
                shape.Radius = sphere.Radius;
            }
            return shape;
        }

        private T Resolve<T>(ResourceReference r, string element) where T : ModResource
        {
            if (r == null || string.IsNullOrEmpty(r.Name)) return null;
            var res = GetResource(r) as T;
            if (res == null) LogMissing(element, "resource '" + r.Name + "'");
            return res;
        }

        private ModAssetBundle ResolveBundle(ResourceReference r)
        {
            if (r == null || string.IsNullOrEmpty(r.Name)) return null;
            if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                var mac = GetResource(new ResourceReference { Name = r.Name + "Mac" }) as ModAssetBundle;
                if (mac != null) return mac;
            }
            return Resolve<ModAssetBundle>(r, "AssetBundle");
        }

        private GameObject Prefab(string name)
        {
            if (string.IsNullOrEmpty(name) || name == "none") return null;
            if (_effectBundle == null) { LogMissing("AssetBundle", "AssetBundle (needed for '" + name + "')"); return null; }
            if (_effectBundle.HasError) { LogMissing("AssetBundle", "AssetBundle (load error: " + _effectBundle.Error + ")"); return null; }
            var prefab = BundleAssets.Load<GameObject>(_effectBundle, name);
            if (prefab == null) LogMissing("AssetBundle", "prefab '" + name + "' in the AssetBundle");
            return prefab;
        }

        private void AddClips(List<ResourceReference> refs, List<AudioClip> into, string element)
        {
            if (refs == null) return;
            foreach (var r in refs)
            {
                var clip = Resolve<ModAudioClip>(r, element);
                if (clip != null && clip.AudioClip != null) into.Add(clip.AudioClip);
            }
        }

        private void LogMissing(string element, string what)
        {
            Debug.LogWarning("[EWL] " + name + " EwlWeapon/" + element + ": " + what + " not found in the block's mod.");
        }
    }
}
