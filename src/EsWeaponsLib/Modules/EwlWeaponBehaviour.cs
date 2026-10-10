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
			projectile.Friction = projectileXml.Friction;
			projectile.Bounciness = projectileXml.Bounciness;
			projectile.FrictionCombine = projectileXml.FrictionCombine;
			projectile.BounceCombine = projectileXml.BounceCombine;
			if (projectileXml.Colliders != null)
				foreach (var c in projectileXml.Colliders) projectile.Colliders.Add(ToShape(c));
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

		private static ColliderShape ToShape(ModCollider c)
		{
			var shape = new ColliderShape { Position = c.Position };
			var cap = c as CapsuleModCollider;
			var box = c as BoxModCollider;
			var sphere = c as SphereModCollider;
			if (cap != null)
			{
				shape.Kind = ColliderKind.Capsule;
				shape.Rotation = cap.Rotation;
				shape.Radius = cap.Radius;
				shape.Height = cap.Height;
				shape.Direction = (int)cap.Dir;
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

		private GameObject Prefab(ModAssetBundle bundle, string name)
		{
			if (string.IsNullOrEmpty(name) || name == "none") return null;
			if (bundle == null) { LogMissing("Effects", "AssetBundle (needed for '" + name + "')"); return null; }
			var go = bundle.LoadAsset<GameObject>(name);
			if (go == null) LogMissing("Effects", "prefab '" + name + "' in the AssetBundle");
			return go;
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
