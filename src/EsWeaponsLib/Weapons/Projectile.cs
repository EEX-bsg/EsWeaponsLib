using System.Collections;
using EWL.Api;
using EWL.Besiege;
using EWL.Effects;
using UnityEngine;

namespace EWL.Weapons
{
	// 弾1発。シミュ権威側にだけ存在する(MP 同期は未実装)。ProjectilePool が組み立て・貸し出し・回収を行う。
	// シミュが終わったら次のフレームで回収される。
	internal sealed class Projectile : MonoBehaviour
	{
		private const float ColliderEnableDelay = 0.02f; // 発射直後の自機誤爆防止(ACM と同じ)
		private const float RemoveAfterHit = 1f;         // 爆発も即消滅もしない弾が命中後に消えるまで(ACM と同じ)
		private const float TimeFuseRandom = 0.05f;      // 時限信管起爆の乱数幅(ACM と同じ)
		private const float DefaultRadius = 0.1f;

		private static Mesh _sphereMesh;

		internal ProjectilePool.Pool Home;

		private Rigidbody _rb;
		private Collider[] _colliders;
		private Transform _visual;
		private MeshFilter _meshFilter;
		private MeshRenderer _renderer;

		private WeaponDriver _owner;
		private Object _simulationScope;
		private WeaponSpec _spec;
		private Collider[] _ignored;
		private PooledEffect _trail, _bullet;
		private float _age;
		private bool _active, _collidersOn, _hit, _detonating;

		/// <summary>弾のオブジェクトを組み立てる(非アクティブ)。形はここで固定。</summary>
		internal static Projectile Build(Transform parent, ProjectileSpec spec)
		{
			var projectileObject = new GameObject("EwlProjectile");
			projectileObject.SetActive(false);
			projectileObject.transform.SetParent(parent, false);
			var self = projectileObject.AddComponent<Projectile>();
			self._colliders = BuildColliders(projectileObject.transform, spec);
			var visual = new GameObject("Vis");
			visual.transform.SetParent(projectileObject.transform, false);
			self._visual = visual.transform;
			self._meshFilter = visual.AddComponent<MeshFilter>();
			self._renderer = visual.AddComponent<MeshRenderer>();
			self._rb = projectileObject.AddComponent<Rigidbody>();
			return self;
		}

		/// <summary>発射。発射ごとに Spec の値(質量・見た目・物理マテリアル等)を設定し直す。</summary>
		internal void Launch(WeaponDriver owner, WeaponSpec spec, Vector3 pos, Quaternion rot, Vector3 velocity,
			Collider[] ignore, PhysicMaterial physicMaterial, Material material)
		{
			var projectileSpec = spec.Projectile;
			_owner = owner;
			_spec = spec;
			_simulationScope = Simulation.Scope;
			_age = 0f;
			_active = true;
			_collidersOn = _hit = _detonating = false;

			transform.SetParent(null, false);
			transform.position = pos;
			transform.rotation = rot;
			transform.localScale = Vector3.one;

			foreach (var collider in _colliders)
			{
				collider.enabled = false;
				collider.sharedMaterial = physicMaterial;
			}
			ApplyVisual(projectileSpec, material);

			gameObject.SetActive(true);
			_ignored = ignore;
			SetIgnore(true); // 非アクティブ中に呼ぶと効かないことがあるのでアクティブ化の後
			_rb.isKinematic = false;
			_rb.mass = projectileSpec.Mass;
			_rb.drag = projectileSpec.Drag;
			_rb.angularDrag = projectileSpec.AngularDrag;
			_rb.useGravity = !projectileSpec.IgnoreGravity;
			_rb.collisionDetectionMode = projectileSpec.CollisionMode;
			_rb.velocity = velocity;
			_rb.angularVelocity = Vector3.zero;

			int effectLimit = projectileSpec.PoolSize;
			_trail = EffectPlayer.Attach(spec.Effects.Trail, transform, _simulationScope, effectLimit);
			_bullet = EffectPlayer.Attach(spec.Effects.Bullet, transform, _simulationScope, effectLimit);
		}

		private static Collider[] BuildColliders(Transform root, ProjectileSpec spec)
		{
			var shapes = spec.Colliders;
			var result = new Collider[shapes.Count > 0 ? shapes.Count : 1];
			if (shapes.Count == 0)
			{
				var sphere = root.gameObject.AddComponent<SphereCollider>();
				sphere.radius = DefaultRadius;
				result[0] = sphere;
				return result;
			}
			for (int i = 0; i < shapes.Count; i++)
			{
				var shape = shapes[i];
				var child = new GameObject("Collider");
				child.transform.SetParent(root, false);
				child.transform.localPosition = shape.Position;
				child.transform.localRotation = Quaternion.Euler(shape.Rotation);
				Collider collider;
				if (shape.Kind == ColliderKind.Box)
				{
					child.transform.localScale = shape.Size; // 本体の BoxModCollider と同じくスケールで大きさを表す
					collider = child.AddComponent<BoxCollider>();
				}
				else if (shape.Kind == ColliderKind.Capsule)
				{
					var capsule = child.AddComponent<CapsuleCollider>();
					capsule.radius = shape.Radius;
					capsule.height = shape.Height;
					capsule.direction = shape.Direction;
					collider = capsule;
				}
				else
				{
					var sphere = child.AddComponent<SphereCollider>();
					sphere.radius = shape.Radius;
					collider = sphere;
				}
				result[i] = collider;
			}
			return result;
		}

		private void ApplyVisual(ProjectileSpec spec, Material material)
		{
			if (spec.Mesh != null)
			{
				_visual.localPosition = spec.MeshPosition;
				_visual.localRotation = Quaternion.Euler(spec.MeshRotation);
				_visual.localScale = spec.MeshScale;
				_meshFilter.sharedMesh = spec.Mesh;
			}
			else
			{
				_visual.localPosition = Vector3.zero;
				_visual.localRotation = Quaternion.identity;
				_visual.localScale = Vector3.one * (DefaultRadius * 2f);
				_meshFilter.sharedMesh = SphereMesh();
			}
			_renderer.sharedMaterial = material;
		}

		private void SetIgnore(bool ignore)
		{
			if (_ignored == null) return;
			foreach (var collider in _colliders)
				foreach (var other in _ignored)
					if (other != null) Physics.IgnoreCollision(collider, other, ignore); // シミュ中に破棄済みのことがある
		}

		private static Mesh SphereMesh()
		{
			if (_sphereMesh != null) return _sphereMesh;
			var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			_sphereMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
			Destroy(primitive);
			return _sphereMesh;
		}

		private void Update()
		{
			if (_simulationScope == null) { Despawn(); return; } // シミュ終了(武装ブロックが壊れても弾は飛び続ける)
			_age += Time.deltaTime;
			if (!_collidersOn && _age >= ColliderEnableDelay)
			{
				_collidersOn = true;
				foreach (var collider in _colliders) collider.enabled = true;
			}
			var projectileSpec = _spec.Projectile;
			if (!_detonating && projectileSpec.TimeFuse > 0f && _age >= projectileSpec.TimeFuse)
				Detonate(Random.Range(0f, TimeFuseRandom), transform.rotation);
			if (_age >= projectileSpec.Lifetime) Despawn();
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (_hit || _detonating) return;
			var other = collision.collider.GetComponentInParent<Projectile>();
			if (other != null && other._owner == _owner) return; // 同じ武装の弾同士は無視
			_hit = true;

			var impact = _spec.Impact;
			var attachTo = Damage.ApplyHit(collision.collider, _rb.velocity, impact.ImpactForce, impact.BlockDamage, impact.EntityDamage, impact.Attach);
			if (impact.Attach) Stick(attachTo);
			EffectPlayer.Fade(_trail);
			_trail = null;

			if (impact.ExplosionRadius > 0f)
			{
				var normal = collision.contacts.Length > 0 ? collision.contacts[0].normal : -transform.forward;
				Detonate(Random.Range(impact.FuseDelay, impact.FuseDelay + impact.FuseRandom), Quaternion.FromToRotation(Vector3.forward, normal));
			}
			else if (impact.DespawnOnHit) Despawn();
			else StartCoroutine(DespawnAfter(RemoveAfterHit));
		}

		private void Stick(Transform target)
		{
			_rb.velocity = Vector3.zero;
			_rb.angularVelocity = Vector3.zero;
			_rb.isKinematic = true;
			if (target == null) return;
			transform.SetParent(target, true);
			// 親のスケールを打ち消して見た目の大きさを保つ
			var lossy = transform.lossyScale;
			if (lossy.x == 0f || lossy.y == 0f || lossy.z == 0f) return;
			var local = transform.localScale;
			transform.localScale = new Vector3(local.x / lossy.x, local.y / lossy.y, local.z / lossy.z);
		}

		private void Detonate(float delay, Quaternion effectRotation)
		{
			_detonating = true;
			StartCoroutine(DetonateAfter(delay, effectRotation));
		}

		private IEnumerator DetonateAfter(float delay, Quaternion effectRotation)
		{
			if (delay > 0f) yield return new WaitForSeconds(delay);
			var impact = _spec.Impact;
			var effects = _spec.Effects;
			var pos = transform.position;
			if (impact.ExplosionRadius > 0f)
			{
				Damage.Explode(pos, impact.ExplosionRadius, impact.ExplosionPower, impact.ExplosionUpPower);
				EffectPlayer.PlayOnce(effects.Explosion, null, pos, effects.ExplosionFacesNormal ? effectRotation : Quaternion.identity,
					Vector3.one, _simulationScope, _spec.Projectile.PoolSize);
				EffectPlayer.PlaySound(effects.ExplosionSounds, pos, Sounds.ExplosionTemplate);
				Despawn();
			}
			else
			{
				if (!impact.DespawnOnHit) yield return new WaitForSeconds(RemoveAfterHit);
				Despawn();
			}
		}

		private IEnumerator DespawnAfter(float delay)
		{
			yield return new WaitForSeconds(delay);
			Despawn();
		}

		/// <summary>プールへ戻す。プールの上限で最古の弾を回収するときにも呼ばれる。</summary>
		internal void Despawn()
		{
			if (!_active) return;
			_active = false;
			StopAllCoroutines();
			EffectPlayer.Fade(_trail);
			EffectPlayer.Return(_bullet);
			_trail = _bullet = null;
			SetIgnore(false);
			_ignored = null;
			_owner = null;
			_spec = null;
			_simulationScope = null;
			ProjectilePool.Release(this);
		}

		private void OnDestroy()
		{
			ProjectilePool.Forget(this);
		}
	}
}
