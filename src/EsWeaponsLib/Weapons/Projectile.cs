using System.Collections;
using EWL.Api;
using EWL.Besiege;
using EWL.Effects;
using UnityEngine;

namespace EWL.Weapons
{
	// 弾1発。シミュ権威側にだけ存在する(MP 同期は未実装)。
	// ponytail: 発射ごとに生成・破棄(プールなし)。連射で GC が問題になったらプール化。
	internal sealed class Projectile : MonoBehaviour
	{
		private const float ColliderEnableDelay = 0.02f; // 発射直後の自機誤爆防止(ACM と同じ)
		private const float RemoveAfterHit = 1f;         // 爆発も即消滅もしない弾が命中後に消えるまで(ACM と同じ)
		private const float TimeFuseRandom = 0.05f;      // 時限信管起爆の乱数幅(ACM と同じ)
		private const float DefaultRadius = 0.1f;

		private static Mesh _sphereMesh;

		private WeaponDriver _owner;
		private WeaponSpec _spec;
		private Rigidbody _rb;
		private Collider[] _colliders;
		private GameObject _trail;
		private float _age;
		private bool _collidersOn, _hit, _detonating;

		internal static void Spawn(WeaponDriver owner, WeaponSpec spec, Vector3 pos, Quaternion rot, Vector3 velocity,
			Collider[] ignore, PhysicMaterial physicMaterial, Material material)
		{
			var projectileSpec = spec.Projectile;
			var go = new GameObject("EwlProjectile");
			go.transform.SetParent(Simulation.Root, false);
			go.transform.position = pos;
			go.transform.rotation = rot;

			var self = go.AddComponent<Projectile>();
			self._owner = owner;
			self._spec = spec;
			self._colliders = BuildColliders(go.transform, projectileSpec, physicMaterial);
			foreach (var c in self._colliders)
			{
				c.enabled = false;
				foreach (var o in ignore) if (o != null) Physics.IgnoreCollision(c, o); // シミュ中に破棄済みのことがある
			}
			BuildVisual(go.transform, projectileSpec, material);

			var rb = self._rb = go.AddComponent<Rigidbody>();
			rb.mass = projectileSpec.Mass;
			rb.drag = projectileSpec.Drag;
			rb.angularDrag = projectileSpec.AngularDrag;
			rb.useGravity = !projectileSpec.IgnoreGravity;
			rb.collisionDetectionMode = projectileSpec.CollisionMode;
			rb.velocity = velocity;

			self._trail = EffectPlayer.Attach(spec.Effects.Trail, go.transform);
			EffectPlayer.Attach(spec.Effects.Bullet, go.transform);
		}

		private static Collider[] BuildColliders(Transform root, ProjectileSpec projectileSpec, PhysicMaterial mat)
		{
			var shapes = projectileSpec.Colliders;
			var result = new Collider[shapes.Count > 0 ? shapes.Count : 1];
			if (shapes.Count == 0)
			{
				var sphere = root.gameObject.AddComponent<SphereCollider>();
				sphere.radius = DefaultRadius;
				sphere.sharedMaterial = mat;
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
				Collider c;
				if (shape.Kind == ColliderKind.Box)
				{
					child.transform.localScale = shape.Size; // 本体の BoxModCollider と同じくスケールで大きさを表す
					c = child.AddComponent<BoxCollider>();
				}
				else if (shape.Kind == ColliderKind.Capsule)
				{
					var cap = child.AddComponent<CapsuleCollider>();
					cap.radius = shape.Radius;
					cap.height = shape.Height;
					cap.direction = shape.Direction;
					c = cap;
				}
				else
				{
					var sphere = child.AddComponent<SphereCollider>();
					sphere.radius = shape.Radius;
					c = sphere;
				}
				c.sharedMaterial = mat;
				result[i] = c;
			}
			return result;
		}

		private static void BuildVisual(Transform root, ProjectileSpec projectileSpec, Material material)
		{
			var vis = new GameObject("Vis");
			vis.transform.SetParent(root, false);
			if (projectileSpec.Mesh != null)
			{
				vis.transform.localPosition = projectileSpec.MeshPosition;
				vis.transform.localRotation = Quaternion.Euler(projectileSpec.MeshRotation);
				vis.transform.localScale = projectileSpec.MeshScale;
			}
			else vis.transform.localScale = Vector3.one * (DefaultRadius * 2f);
			vis.AddComponent<MeshFilter>().sharedMesh = projectileSpec.Mesh != null ? projectileSpec.Mesh : SphereMesh();
			vis.AddComponent<MeshRenderer>().sharedMaterial = material;
		}

		private static Mesh SphereMesh()
		{
			if (_sphereMesh != null) return _sphereMesh;
			var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			_sphereMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
			Destroy(tmp);
			return _sphereMesh;
		}

		private void Update()
		{
			_age += Time.deltaTime;
			if (!_collidersOn && _age >= ColliderEnableDelay)
			{
				_collidersOn = true;
				foreach (var c in _colliders) c.enabled = true;
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
				EffectPlayer.PlayOnce(effects.Explosion, null, pos, effects.ExplosionFacesNormal ? effectRotation : Quaternion.identity, Vector3.one);
				EffectPlayer.PlaySound(effects.ExplosionSounds, pos);
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

		private void Despawn()
		{
			EffectPlayer.Fade(_trail);
			_trail = null;
			Destroy(gameObject);
		}
	}
}
