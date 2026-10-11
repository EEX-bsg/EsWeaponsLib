using System.Collections;
using System.Collections.Generic;
using EWL.Api;
using EWL.Besiege;
using EWL.Effects;
using EWL.Net;
using UnityEngine;

namespace EWL.Weapons
{
    // 弾1発。ProjectilePool が組み立て・貸し出し・回収を行う。シミュが終わったら次のフレームで回収される。
    // 実弾はシミュ権威側にだけ存在する。MP クライアントではホストの弾の見た目だけを出すプロキシとして使う(当たり判定なし)。
    internal sealed class Projectile : MonoBehaviour
    {
        private const float ColliderEnableDelay = 0.02f; // 発射直後の自機誤爆防止(ACM と同じ)
        private const float RemoveAfterHit = 1f;         // 爆発も即消滅もしない弾が命中後に消えるまで(ACM と同じ)
        private const float TimeFuseRandom = 0.05f;      // 時限信管起爆の乱数幅(ACM と同じ)
        private const float MaxExtrapolation = 0.5f;     // プロキシ: スナップショットが途絶えたら外挿を止める
        private const float CorrectionRate = 10f;        // プロキシ: スナップショットとのずれを詰める速さ [1/s]
        internal const float DefaultRadius = 0.1f;

        private static Mesh _sphereMesh;

        internal ProjectilePool.Pool Home;
        /// <summary>MP の弾 ID。0 なら配信していない(ホスト)/登録されていない(クライアント)。</summary>
        internal int NetworkId;
        /// <summary>プロキシ: 生成されたときのホストのスナップショット番号。</summary>
        internal int SpawnFrame;

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
        private float _timeFuse; // 発射時の値(スライダーで変わっても飛行中の弾は変えない)
        private bool _active, _collidersOn, _hit, _detonating;
        // 撃った側(命中イベント用。発射時点の値)
        private Weapon _weapon;
        private BlockBehaviour _shooterBlock;
        private ushort _shooterPlayerId;
        private MPTeam _shooterTeam;
        // プロキシ
        private bool _proxy;
        private Vector3 _networkPosition, _networkVelocity, _correction;
        private float _networkTime;

        internal Vector3 Velocity { get { return _rb.velocity; } }

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
            Begin(owner, spec, pos, rot, material);
            _weapon = owner.Owner;
            _shooterBlock = owner.Block;
            _shooterPlayerId = Simulation.PlayerIdOf(owner.Block);
            _shooterTeam = owner.Block.Team;
            foreach (var collider in _colliders) collider.sharedMaterial = physicMaterial;

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
            AttachEffects();
        }

        /// <summary>MP クライアント: ホストの弾の見た目だけを出す。当たり判定・物理なし、位置はスナップショットで外挿する。</summary>
        internal void LaunchProxy(WeaponDriver owner, WeaponSpec spec, Vector3 pos, Quaternion rot, Vector3 velocity, Material material)
        {
            Begin(owner, spec, pos, rot, material);
            _proxy = true;
            _collidersOn = true; // 有効化しない
            _networkPosition = pos;
            _networkVelocity = velocity;
            _correction = Vector3.zero;
            _networkTime = Time.time;
            gameObject.SetActive(true);
            _rb.collisionDetectionMode = CollisionDetectionMode.Discrete; // キネマティックに連続判定は使えない
            _rb.isKinematic = true;
            AttachEffects();
        }

        private void Begin(WeaponDriver owner, WeaponSpec spec, Vector3 pos, Quaternion rot, Material material)
        {
            _owner = owner;
            _spec = spec;
            _simulationScope = owner.Scope;
            _age = 0f;
            _timeFuse = spec.Projectile.TimeFuse;
            _active = true;
            _proxy = _collidersOn = _hit = _detonating = false;

            transform.SetParent(null, false);
            transform.position = pos;
            transform.rotation = rot;
            transform.localScale = Vector3.one;
            foreach (var collider in _colliders) collider.enabled = false;
            ApplyVisual(spec.Projectile, material);
        }

        private void AttachEffects()
        {
            int effectLimit = _spec.Projectile.PoolSize;
            _trail = EffectPlayer.Attach(_spec.Effects.Trail, transform, _simulationScope, effectLimit);
            _bullet = EffectPlayer.Attach(_spec.Effects.Bullet, transform, _simulationScope, effectLimit);
        }

        internal void ReceiveSnapshot(Vector3 position, Vector3 velocity)
        {
            var shown = transform.position;
            _networkPosition = position;
            _networkVelocity = velocity;
            _networkTime = Time.time;
            _correction = shown - position; // 見た目を飛ばさず、ずれを徐々に詰める
        }

        /// <summary>ホストで弾が消えた。爆発していれば同じ位置・向きでエフェクトと音を出す。</summary>
        internal void EndProxy(bool exploded, Vector3 position, Quaternion effectRotation)
        {
            if (exploded)
            {
                var effects = _spec.Effects;
                EffectPlayer.PlayOnce(effects.Explosion, null, position, effectRotation, Vector3.one, _simulationScope, _spec.Projectile.PoolSize);
                EffectPlayer.PlaySound(effects.ExplosionSounds, position, Sounds.ExplosionTemplate);
            }
            Despawn();
        }

        private void UpdateProxy()
        {
            _age += Time.deltaTime;
            float elapsed = Mathf.Min(Time.time - _networkTime, MaxExtrapolation);
            var predicted = _networkPosition + _networkVelocity * elapsed;
            if (!_spec.Projectile.IgnoreGravity) predicted += Physics.gravity * (0.5f * elapsed * elapsed);
            _correction *= Mathf.Exp(-CorrectionRate * Time.deltaTime);
            transform.position = predicted + _correction;
            // 取りこぼし対策の上限(通常は Despawn か生存一覧で消える)
            if (_age >= _spec.Projectile.Lifetime + 2f) Despawn();
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

        internal static Mesh SphereMesh()
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
            if (_proxy) { UpdateProxy(); return; }
            _age += Time.deltaTime;
            if (!_collidersOn && _age >= ColliderEnableDelay)
            {
                _collidersOn = true;
                foreach (var collider in _colliders) collider.enabled = true;
            }
            var projectileSpec = _spec.Projectile;
            if (!_detonating && _timeFuse > 0f && _age >= _timeFuse)
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
            if (Weapon.HasHitListeners(_weapon))
            {
                var point = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
                RaiseHit(Damage.BlockOf(collision.collider), collision.collider, point, false);
            }
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
                var affected = Weapon.HasHitListeners(_weapon) ? new List<Rigidbody>() : null;
                Damage.Explode(pos, impact.ExplosionRadius, impact.ExplosionPower, impact.ExplosionUpPower, affected);
                if (affected != null)
                    foreach (var body in affected) RaiseHit(body.GetComponent<BlockBehaviour>(), null, pos, true);
                var rotation = effects.ExplosionFacesNormal ? effectRotation : Quaternion.identity;
                EffectPlayer.PlayOnce(effects.Explosion, null, pos, rotation, Vector3.one, _simulationScope, _spec.Projectile.PoolSize);
                EffectPlayer.PlaySound(effects.ExplosionSounds, pos, Sounds.ExplosionTemplate);
                if (NetworkId != 0) ProjectileSync.Despawned(this, true, pos, rotation);
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

        private void RaiseHit(BlockBehaviour block, Collider collider, Vector3 point, bool isExplosion)
        {
            Weapon.RaiseHit(new ProjectileHit
            {
                Weapon = _weapon,
                ShooterBlock = _shooterBlock,
                ShooterPlayerId = _shooterPlayerId,
                ShooterTeam = _shooterTeam,
                Block = block,
                Collider = collider,
                Point = point,
                IsExplosion = isExplosion,
            });
        }

        /// <summary>プールへ戻す。プールの上限で最古の弾を回収するときにも呼ばれる。</summary>
        internal void Despawn()
        {
            if (!_active) return;
            _active = false;
            NotifyNetworkRemoved();
            StopAllCoroutines();
            EffectPlayer.Fade(_trail);
            EffectPlayer.Return(_bullet);
            _trail = _bullet = null;
            SetIgnore(false);
            _ignored = null;
            _owner = null;
            _spec = null;
            _simulationScope = null;
            _weapon = null;
            _shooterBlock = null;
            ProjectilePool.Release(this);
        }

        private void NotifyNetworkRemoved()
        {
            if (NetworkId == 0) return;
            if (_proxy) ProjectileSync.ProxyRemoved(this);
            else ProjectileSync.Despawned(this, false, transform.position, transform.rotation);
        }

        private void OnDestroy()
        {
            if (_active) NotifyNetworkRemoved(); // 刺さった先ごと破棄された
            ProjectilePool.Forget(this);
        }
    }
}
