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
        private MKey _fireKey;
        private MToggle _holdToggle;
        private MKey _reloadKey;
        private MToggle _autoReloadToggle;
        private MSlider _delaySlider;
        private MSlider _timeFuseSlider;
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
            if (Module.DelaySlider != null) _delaySlider = GetSlider(Module.DelaySlider);
            if (Module.TimeFuseSlider != null) _timeFuseSlider = GetSlider(Module.TimeFuseSlider);
        }

        public override void OnSimulateStart()
        {
            _autoFire = _fireHeldBefore = _reloadHeldBefore = false;
            _weapon = Weapon.Create(BlockBehaviour, BuildSpec());
            _effectBundle = ResolveBundle(Module.Effects.AssetBundle);
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
            bool hold = _holdToggle != null ? _holdToggle.IsActive : Module.Fire.Hold;
            bool fireHeld = IsHeld(_fireKey);
            bool firePressed = fireHeld && !_fireHeldBefore;
            _fireHeldBefore = fireHeld;
            if (!hold && firePressed) _autoFire = !_autoFire;
            if (hold ? fireHeld : _autoFire)
            {
                ApplySliders(_weapon.Spec);
                _weapon.TryFire();
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
            var spec = new WeaponSpec();

            var fire = spec.Fire;
            fire.MuzzlePosition = Module.Muzzle.Position;
            fire.MuzzleRotation = Quaternion.Euler(Module.Muzzle.Rotation);
            fire.Velocity = Module.Fire.Velocity;
            fire.Interval = Module.Fire.Interval;
            fire.Spread = Module.Fire.Spread;
            fire.Delay = Module.Fire.Delay;
            fire.RandomDelay = Module.Fire.RandomDelay;
            fire.Recoil = Module.Fire.Recoil;
            fire.Ammo = Module.Fire.Ammo;
            fire.BurstCount = Module.Fire.BurstCount;
            fire.BurstInterval = Module.Fire.BurstInterval;
            fire.MagazineCapacity = Module.Fire.MagazineCapacity;
            fire.ReloadTime = Module.Fire.ReloadTime;
            fire.AutoReload = _autoReloadToggle != null ? _autoReloadToggle.IsActive : Module.Fire.AutoReload;
            fire.EjectVelocity = Module.Fire.EjectVelocity;

            var boosterXml = Module.Booster;
            spec.Booster.Thrust = boosterXml.Thrust;
            spec.Booster.BurnTime = boosterXml.BurnTime;
            spec.Booster.LateralDrag = boosterXml.LateralDrag;

            var projectileXml = Module.Projectile;
            var projectile = spec.Projectile;
            projectile.Mass = projectileXml.Mass;
            projectile.Drag = projectileXml.Drag;
            projectile.AngularDrag = projectileXml.AngularDrag;
            projectile.IgnoreGravity = projectileXml.IgnoreGravity;
            projectile.CollisionMode = projectileXml.CollisionMode;
            projectile.Lifetime = projectileXml.Lifetime;
            projectile.TimeFuse = projectileXml.TimeFuse;
            projectile.PoolSize = projectileXml.PoolSize;
            projectile.PoolKey = PoolKeyFor(Module);
            projectile.Friction = projectileXml.Friction;
            projectile.Bounciness = projectileXml.Bounciness;
            projectile.FrictionCombine = projectileXml.FrictionCombine;
            projectile.BounceCombine = projectileXml.BounceCombine;
            projectile.ShowWhenLoaded = projectileXml.ShowWhenLoaded;
            if (projectileXml.Colliders != null)
                foreach (var collider in projectileXml.Colliders) projectile.Colliders.Add(ToShape(collider));
            if (projectileXml.Mesh != null)
            {
                var mesh = Resolve<ModMesh>(projectileXml.Mesh, "Projectile/Mesh");
                projectile.Mesh = mesh != null ? mesh.Mesh : null;
                projectile.MeshPosition = projectileXml.Mesh.Position;
                projectile.MeshRotation = projectileXml.Mesh.Rotation;
                projectile.MeshScale = projectileXml.Mesh.Scale;
            }
            var tex = Resolve<ModTexture>(projectileXml.Texture, "Projectile/Texture");
            projectile.Texture = tex != null ? tex.Texture : null;

            var impactXml = Module.Impact;
            var impact = spec.Impact;
            impact.BlockDamage = impactXml.BlockDamage;
            impact.EntityDamage = impactXml.EntityDamage;
            impact.ImpactForce = impactXml.ImpactForce;
            impact.Attach = impactXml.Attach;
            impact.ExplosionRadius = impactXml.ExplosionRadius;
            impact.ExplodesOnlyWithGodTool = impactXml.ExplodesOnlyWithGodTool;
            impact.ExplosionPower = impactXml.ExplosionPower;
            impact.ExplosionUpPower = impactXml.ExplosionUpPower;
            impact.FuseDelay = impactXml.FuseDelay;
            impact.FuseRandom = impactXml.FuseRandom;
            impact.DespawnOnHit = impactXml.DespawnOnHit;

            var effectsXml = Module.Effects;
            var effects = spec.Effects;
            effects.ShotFlashPosition = effectsXml.ShotFlashPosition;
            effects.ShotFlashRotation = effectsXml.ShotFlashRotation;
            effects.ShotFlashScale = effectsXml.ShotFlashScale;
            effects.ExplosionFacesNormal = effectsXml.ExplosionFacesNormal;
            AddClips(effectsXml.FireSounds, effects.FireSounds, "Effects/FireSounds");
            AddClips(effectsXml.ExplosionSounds, effects.ExplosionSounds, "Effects/ExplosionSounds");
            AddClips(effectsXml.ReloadSounds, effects.ReloadSounds, "Effects/ReloadSounds");
            ApplySliders(spec);
            return spec;
        }

        // スライダーはシミュ中に他のモジュールが動かすことがある(キーで信管時間を変える等)ので、発射ごとに読み直す。
        private void ApplySliders(WeaponSpec spec)
        {
            if (_delaySlider != null) spec.Fire.Delay = Module.Fire.Delay + _delaySlider.Value;
            if (_timeFuseSlider != null) spec.Projectile.TimeFuse = _timeFuseSlider.Value;
        }

        // Spec は発射のたびに読まれるので、シミュ中に差し込んでも次の弾から反映される。
        private void SetEffectPrefabs()
        {
            if (this == null || _weapon == null) return; // OnSimulateStop を経ずに破棄された後の OnLoad
            var effectsXml = Module.Effects;
            var effects = _weapon.Spec.Effects;
            effects.ShotFlash = Prefab(effectsXml.ShotFlash);
            effects.Trail = Prefab(effectsXml.Trail);
            effects.Bullet = Prefab(effectsXml.Bullet);
            effects.Explosion = Prefab(effectsXml.Explosion);
        }

        // モジュールのインスタンスはブロック定義ごとに1つなので、同じブロック種別の武装は弾のプールを共有する。
        private static readonly Dictionary<EwlWeaponModule, string> PoolKeys = new Dictionary<EwlWeaponModule, string>();

        private static string PoolKeyFor(EwlWeaponModule module)
        {
            string key;
            if (!PoolKeys.TryGetValue(module, out key)) PoolKeys[module] = key = "EwlWeapon#" + PoolKeys.Count;
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
            return Resolve<ModAssetBundle>(r, "Effects/AssetBundle");
        }

        private GameObject Prefab(string name)
        {
            if (string.IsNullOrEmpty(name) || name == "none") return null;
            if (_effectBundle == null) { LogMissing("Effects", "AssetBundle (needed for '" + name + "')"); return null; }
            if (_effectBundle.HasError) { LogMissing("Effects", "AssetBundle (load error: " + _effectBundle.Error + ")"); return null; }
            var prefab = BundleAssets.Load<GameObject>(_effectBundle, name);
            if (prefab == null) LogMissing("Effects", "prefab '" + name + "' in the AssetBundle");
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
