using System;
using System.Collections;
using System.Collections.Generic;
using EWL.Api;
using EWL.Besiege;
using EWL.Effects;
using UnityEngine;

namespace EWL.Weapons
{
	// 武装1基の実体。ブロックの GameObject に付く。公開面は Api.Weapon が包む。
	internal sealed class WeaponDriver : MonoBehaviour
	{
		internal Weapon Owner;
		internal WeaponSpec Spec;
		internal Action Fired;
		internal int Ammo;

		private BlockBehaviour _block;
		private Collider[] _ownColliders;
		private float _nextFireTime;
		private PhysicMaterial _physicMaterial;
		private Material _material;
		private Texture _materialTexture;
		private string _privatePoolKey;
		private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();

		internal void Init(BlockBehaviour block, WeaponSpec spec)
		{
			_block = block;
			Spec = spec;
			Ammo = spec.Fire.Ammo;
			_ownColliders = block.GetComponentsInChildren<Collider>();
			_privatePoolKey = "weapon#" + GetInstanceID();
			ProjectilePool.Prewarm(PoolKey, spec.Projectile);
		}

		private void OnDestroy()
		{
			if (_privatePoolKey != null) ProjectilePool.Discard(_privatePoolKey);
			foreach (var obj in _owned) Destroy(obj);
			_owned.Clear();
		}

		/// <summary>PoolKey 未指定ならこの武装専用のプール。</summary>
		private string PoolKey
		{
			get
			{
				var key = Spec.Projectile.PoolKey;
				return string.IsNullOrEmpty(key) ? _privatePoolKey : key;
			}
		}

		internal bool TryFire()
		{
			if (!Simulation.HasAuthority(_block) || Time.time < _nextFireTime) return false;
			bool infinite = Simulation.IsInfiniteAmmo(_block);
			if (Ammo == 0 && !infinite) return false;

			var fire = Spec.Fire;
			_nextFireTime = Time.time + fire.Interval;
			if (Ammo > 0 && !infinite) Ammo--;
			if (Fired != null) Fired();
			StartCoroutine(Shoot(fire.Delay + UnityEngine.Random.Range(0f, fire.RandomDelay)));
			return true;
		}

		private IEnumerator Shoot(float delay)
		{
			if (delay > 0f) yield return new WaitForSeconds(delay);
			yield return new WaitForFixedUpdate();
			if (_block == null || !Simulation.HasAuthority(_block) || _block.Rigidbody == null) yield break;

			var fire = Spec.Fire;
			var projectile = Spec.Projectile;
			var blockTransform = transform;
			Vector3 pos = blockTransform.TransformPoint(fire.MuzzlePosition);
			Quaternion rot = blockTransform.rotation * fire.MuzzleRotation;
			Vector3 dir = rot * Vector3.forward;
			if (fire.Spread > 0f)
				dir = (dir + UnityEngine.Random.insideUnitSphere * Mathf.Tan(fire.Spread * Mathf.Deg2Rad)).normalized;

			var rb = _block.Rigidbody;
			var physicMaterial = PhysicMaterialFor(projectile);
			var material = MaterialFor(projectile);
			ProjectilePool.Acquire(PoolKey, projectile).Launch(this, Spec, pos, rot, rb.velocity + dir * fire.Velocity, _ownColliders,
				physicMaterial, material);
			rb.AddForce(-dir * (fire.Velocity * projectile.Mass * fire.Recoil), ForceMode.Impulse);

			var effects = Spec.Effects;
			EffectPlayer.PlayOnce(effects.ShotFlash, blockTransform, effects.ShotFlashPosition, Quaternion.Euler(effects.ShotFlashRotation),
				effects.ShotFlashScale, Simulation.Scope, projectile.PoolSize);
			EffectPlayer.PlaySound(effects.FireSounds, blockTransform.TransformPoint(effects.ShotFlashPosition), Sounds.FireTemplate);
		}

		// 発射ごとに Spec を読む。値が変わったら新しく作り、古いもの(飛行中の弾が使用中)は武装の破棄時にまとめて破棄する。
		private PhysicMaterial PhysicMaterialFor(ProjectileSpec projectile)
		{
			var cached = _physicMaterial;
			if (cached != null && cached.dynamicFriction == projectile.Friction && cached.frictionCombine == projectile.FrictionCombine
				&& cached.bounciness == projectile.Bounciness && cached.bounceCombine == projectile.BounceCombine) return cached;
			cached = _physicMaterial = new PhysicMaterial("EwlProjectile")
			{
				staticFriction = projectile.Friction,
				dynamicFriction = projectile.Friction,
				frictionCombine = projectile.FrictionCombine,
				bounciness = projectile.Bounciness,
				bounceCombine = projectile.BounceCombine,
			};
			_owned.Add(cached);
			return cached;
		}

		private Material MaterialFor(ProjectileSpec projectile)
		{
			if (_material != null && _materialTexture == projectile.Texture) return _material;
			// Standard は Besiege のビルドで紫になることがあるので Legacy Diffuse を優先
			var shader = Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Diffuse") ?? Shader.Find("Standard");
			if (shader == null)
			{
				Debug.LogWarning("[EWL] No usable shader for projectile material.");
				return null;
			}
			_material = new Material(shader) { mainTexture = projectile.Texture };
			_materialTexture = projectile.Texture;
			_owned.Add(_material);
			return _material;
		}
	}
}
