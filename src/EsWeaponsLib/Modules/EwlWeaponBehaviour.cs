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
		private Weapon _weapon;
		private bool _autoFire;

		public override void SafeAwake()
		{
			_fireKey = GetKey(Module.FireKey);
			if (Module.HoldToFireToggle != null) _holdToggle = GetToggle(Module.HoldToFireToggle);
		}

		public override void OnSimulateStart()
		{
			_autoFire = false;
			_weapon = Weapon.Create(BlockBehaviour, BuildSpec());
		}

		public override void OnSimulateStop()
		{
			if (_weapon != null) _weapon.Destroy();
			_weapon = null;
		}

		public override void SimulateUpdateHost()
		{
			if (_weapon == null) return;
			bool hold = _holdToggle != null ? _holdToggle.IsActive : Module.Fire.Hold;
			// 実キーとエミュレーション(ロジックブロック等)は別経路
			if (!hold && (_fireKey.IsPressed || _fireKey.EmulationPressed())) _autoFire = !_autoFire;
			bool trigger = hold ? _fireKey.IsHeld || _fireKey.EmulationHeld(true) : _autoFire;
			if (trigger) _weapon.TryFire();
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
			impact.ExplosionPower = impactXml.ExplosionPower;
			impact.ExplosionUpPower = impactXml.ExplosionUpPower;
			impact.FuseDelay = impactXml.FuseDelay;
			impact.FuseRandom = impactXml.FuseRandom;
			impact.DespawnOnHit = impactXml.DespawnOnHit;

			var effectsXml = Module.Effects;
			var effects = spec.Effects;
			var bundle = ResolveBundle(effectsXml.AssetBundle);
			effects.ShotFlash = Prefab(bundle, effectsXml.ShotFlash);
			effects.ShotFlashPosition = effectsXml.ShotFlashPosition;
			effects.ShotFlashRotation = effectsXml.ShotFlashRotation;
			effects.ShotFlashScale = effectsXml.ShotFlashScale;
			effects.Trail = Prefab(bundle, effectsXml.Trail);
			effects.Bullet = Prefab(bundle, effectsXml.Bullet);
			effects.Explosion = Prefab(bundle, effectsXml.Explosion);
			effects.ExplosionFacesNormal = effectsXml.ExplosionFacesNormal;
			AddClips(effectsXml.FireSounds, effects.FireSounds, "Effects/FireSounds");
			AddClips(effectsXml.ExplosionSounds, effects.ExplosionSounds, "Effects/ExplosionSounds");
			return spec;
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

		// ModAssetBundle.LoadAsset は呼ぶたびにバンドルの全アセットを読む(本体の空バンドル検出)ので重い。取り出したプレハブを覚えておく。
		// 本体がバンドルを読み直しても(Unload(false))取り出し済みのアセットは残るので、破棄されていない限りそのまま使える。
		private static readonly Dictionary<ModAssetBundle, Dictionary<string, GameObject>> PrefabCache =
			new Dictionary<ModAssetBundle, Dictionary<string, GameObject>>();

		private GameObject Prefab(ModAssetBundle bundle, string name)
		{
			if (string.IsNullOrEmpty(name) || name == "none") return null;
			if (bundle == null) { LogMissing("Effects", "AssetBundle (needed for '" + name + "')"); return null; }
			Dictionary<string, GameObject> prefabs;
			if (!PrefabCache.TryGetValue(bundle, out prefabs)) PrefabCache[bundle] = prefabs = new Dictionary<string, GameObject>();
			GameObject prefab;
			if (prefabs.TryGetValue(name, out prefab) && prefab != null) return prefab;

			if (!bundle.Loaded) { LogMissing("Effects", "AssetBundle (still loading, needed for '" + name + "')"); return null; }
			if (bundle.HasError) { LogMissing("Effects", "AssetBundle (load error: " + bundle.Error + ")"); return null; }
			prefab = bundle.LoadAsset<GameObject>(name);
			if (prefab == null) LogMissing("Effects", "prefab '" + name + "' in the AssetBundle");
			else prefabs[name] = prefab;
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
