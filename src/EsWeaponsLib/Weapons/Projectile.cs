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
        private const float RemoveAfterHit = 1f;         // 爆発も即消滅もしない弾が命中後に消えるまで(ACM と同じ)
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
        private ProjectileSpec _spec;
        private Collider[] _ignored;
        private PooledEffect _trail, _attachedEffect;
        private MissileGuidance _guidance; // 誘導する弾だけ。弾のオブジェクトと一緒に使い回す
        private float _guidanceGain; // 発射時の値。0 なら誘導しない
        private float _age;
        private Vector3 _flightVelocity; // 直前の物理フレームの速度(命中時の _rb.velocity は衝突後の値)
        private float _timeFuse; // 発射時の値(スライダーで変わっても飛行中の弾は変えない)
        private float _thrustTime; // 発射時の値(同上)
        private float _colliderEnableDelay; // 発射時の値
        private bool _godToolExplosion; // 発射時の値(飛行中に武装ブロックが壊れてもマシンを辿らない)
        private bool _active, _collidersOn, _hit, _detonating;
        // 撃った側(命中イベント用。発射時点の値)
        private Weapon _weapon;
        private BlockBehaviour _shooterBlock;
        private Attacker _shooter;
        // プロキシ
        private bool _proxy;
        private Vector3 _networkPosition, _networkVelocity, _correction;
        private float _networkTime;

        internal Vector3 Velocity { get { return _rb.velocity; } }

        /// <summary>撃った人(実弾のみ。プロキシ・待機中は null)。</summary>
        internal Attacker Shooter { get { return _shooter; } }

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
        internal void Launch(WeaponDriver owner, ProjectileSpec spec, Vector3 pos, Quaternion rot, Vector3 velocity,
            Collider[] ignore, PhysicMaterial physicMaterial, Material material, Transform guidanceTarget)
        {
            Begin(owner, spec, pos, rot, material);
            _weapon = owner.Owner;
            _shooterBlock = owner.Block;
            _shooter = AttackerTags.OfBlock(owner.Block);
            foreach (var collider in _colliders) collider.sharedMaterial = physicMaterial;

            gameObject.SetActive(true);
            _ignored = ignore;
            SetIgnore(true); // 非アクティブ中に呼ぶと効かないことがあるのでアクティブ化の後
            if (_colliderEnableDelay <= 0f) EnableColliders();
            _rb.isKinematic = false;
            _rb.mass = spec.Mass;
            _rb.drag = spec.Drag;
            _rb.angularDrag = spec.AngularDrag;
            _rb.useGravity = !spec.IgnoreGravity;
            _rb.collisionDetectionMode = spec.CollisionMode;
            _rb.velocity = velocity;
            _rb.angularVelocity = Vector3.zero;
            _flightVelocity = velocity;
            if (_guidanceGain > 0f) // 目標は発射要求の時点で確定済み。飛行中は変えない
            {
                if (_guidance == null) _guidance = new MissileGuidance();
                _guidance.Begin(guidanceTarget, rot * Vector3.forward, spec.Guidance.ReleaseAngle, spec.Guidance.PredictionIterations);
            }
            AttachEffects();
        }

        /// <summary>MP クライアント: ホストの弾の見た目だけを出す。当たり判定・物理なし、位置はスナップショットで外挿する。</summary>
        internal void LaunchProxy(WeaponDriver owner, ProjectileSpec spec, Vector3 pos, Quaternion rot, Vector3 velocity, Material material)
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

        private void Begin(WeaponDriver owner, ProjectileSpec spec, Vector3 pos, Quaternion rot, Material material)
        {
            _owner = owner;
            _spec = spec;
            _simulationScope = owner.Scope;
            _age = 0f;
            _colliderEnableDelay = spec.ColliderEnableDelay;
            // 負値・NaN は信管なし扱い(仕様ではない)。無限に乱数を足しても無限
            _timeFuse = spec.TimeFuse >= 0f ? spec.TimeFuse + Random.Range(0f, spec.TimeFuseRandomExtra) : float.PositiveInfinity;
            _thrustTime = spec.Booster.ThrustTime;
            _godToolExplosion = spec.Explosion.OnlyWithExplodingCannonballs && Simulation.IsExplodingCannonballs(owner.Block);
            _active = true;
            _guidanceGain = spec.Guidance.Gain;
            _proxy = _collidersOn = _hit = _detonating = false;

            transform.SetParent(Home.Root, false); // 刺さった先から戻す。置き場は原点・無回転なのでワールド座標のまま扱える
            transform.position = pos;
            transform.rotation = rot;
            transform.localScale = Vector3.one;
            foreach (var collider in _colliders) collider.enabled = false;
            ApplyVisual(spec, material);
        }

        private void AttachEffects()
        {
            int effectLimit = _spec.PoolSize;
            _trail = EffectPlayer.Attach(_spec.Trail, transform, _simulationScope, effectLimit);
            _attachedEffect = EffectPlayer.Attach(_spec.AttachedEffect, transform, _simulationScope, effectLimit);
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
                var explosion = _spec.Explosion;
                EffectPlayer.PlayOnce(explosion.Effect, null, position, effectRotation, Vector3.one, _simulationScope, _spec.PoolSize);
                EffectPlayer.PlaySound(explosion.Sounds, position, Sounds.ExplosionTemplate);
            }
            Despawn();
        }

        private void UpdateProxy()
        {
            _age += Time.deltaTime;
            float elapsed = Mathf.Min(Time.time - _networkTime, MaxExtrapolation);
            // Unity の Drag は速度の指数減衰とみなせる: 移動量 = v (1 - e^-kt) / k
            float drag = _spec.Drag;
            float travelTime = drag > 0.0001f ? (1f - Mathf.Exp(-drag * elapsed)) / drag : elapsed;
            var predicted = _networkPosition + _networkVelocity * travelTime;
            if (!_spec.IgnoreGravity) predicted += Physics.gravity * (0.5f * elapsed * elapsed);
            // 誘導弾・安定翼付きの弾は向きが変わるので、受け取った速度の向きへ回す(スナップショットに向きは無い)
            if ((_guidanceGain > 0f || _spec.Booster.Stability > 0f) && _networkVelocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_networkVelocity), 1f - Mathf.Exp(-CorrectionRate * Time.deltaTime));
            var booster = _spec.Booster;
            if (booster.Thrust > 0f && _age < _thrustTime && _spec.Mass > 0f && BelowMaxSpeed(booster, _networkVelocity))
                predicted += transform.forward * (booster.Thrust / _spec.Mass * 0.5f * elapsed * elapsed);
            _correction *= Mathf.Exp(-CorrectionRate * Time.deltaTime);
            transform.position = predicted + _correction;
            // 取りこぼし対策の上限(通常は Despawn か生存一覧で消える)
            if (_age >= _spec.Lifetime + 2f) Despawn();
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

        private void EnableColliders()
        {
            _collidersOn = true;
            foreach (var collider in _colliders) collider.enabled = true;
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
            if (!_collidersOn && _age >= _colliderEnableDelay) EnableColliders();
            float lifetime = _spec.Lifetime; // 遅延 0 の起爆はこの場で Despawn まで進み _spec が消える
            if (!_detonating && _age >= _timeFuse)
                Detonate(0f, transform.rotation);
            if (_age >= lifetime) Despawn();
        }

        private void FixedUpdate()
        {
            if (!_active || _proxy || _hit || _detonating) return;
            var booster = _spec.Booster;
            bool thrusting = booster.Thrust > 0f && _age < _thrustTime && _spec.Mass > 0f;
            if (_guidanceGain > 0f)
                _guidance.Step(_rb, transform, _guidanceGain, _age, thrusting ? booster.Thrust / _spec.Mass : 0f, booster.MaxSpeed);
            var forward = transform.forward;
            // 風見トルク: forward × velocity は大きさが機首に垂直な速度成分で、機首を速度へ回す軸を向く
            if (booster.Stability > 0f) _rb.AddTorque(Vector3.Cross(forward, _rb.velocity) * booster.Stability, ForceMode.Acceleration);
            // 揚力: 機首に垂直な速度成分を打ち消し、飛ぶ向きを機首へ寄せる(1フレームで打ち消し過ぎて振動しないよう上限を置く)
            if (booster.Lift > 0f)
            {
                var velocity = _rb.velocity;
                float lift = Mathf.Min(booster.Lift, 1f / Time.fixedDeltaTime);
                _rb.AddForce((forward * Vector3.Dot(velocity, forward) - velocity) * lift, ForceMode.Acceleration);
            }
            if (booster.Thrust > 0f && _age < _thrustTime && BelowMaxSpeed(booster, _rb.velocity)) _rb.AddForce(forward * booster.Thrust); // NaN でも力を加えない
            _flightVelocity = _rb.velocity;
        }

        private static bool BelowMaxSpeed(BoosterSpec booster, Vector3 velocity)
        {
            return booster.MaxSpeed <= 0f || velocity.sqrMagnitude < booster.MaxSpeed * booster.MaxSpeed;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hit || _detonating) return;
            var other = collision.collider.GetComponentInParent<Projectile>();
            // 同じ武装の弾同士は無視。参照で比べる(武装が破棄済みだと Unity の == は別々の武装でも一致する)
            if (other != null && ReferenceEquals(other._owner, _owner)) return;
            _hit = true;

            var spec = _spec;
            var hitCollider = collision.collider;
            var point = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            bool penetrated = spec.Penetration > 0f && Penetrate(ref hitCollider, ref point);
            var attachTo = Damage.ApplyHit(hitCollider, _rb.velocity, spec.ImpactForce, spec.BlockDamage, spec.EntityDamage, spec.AttachOnHit, _shooter);
            if (Weapon.HasHitListeners(_weapon))
                RaiseHit(Damage.BlockOf(hitCollider), hitCollider, point, HitKind.Direct);
            if (penetrated)
            {
                // 装甲の中(か抜けた所)で止まり、止まった先のブロックと一緒に動く。中で押し出されないよう当たり判定を切る
                foreach (var collider in _colliders) collider.enabled = false;
                transform.position = point;
                AttachTo(attachTo != null ? attachTo : (hitCollider.attachedRigidbody != null ? hitCollider.attachedRigidbody.transform : null));
            }
            else if (spec.AttachOnHit) AttachTo(attachTo);
            EffectPlayer.Fade(_trail);
            _trail = null;

            if (Explodes)
            {
                var normal = collision.contacts.Length > 0 ? collision.contacts[0].normal : -transform.forward;
                Detonate(Random.Range(spec.DetonationDelay, spec.DetonationDelay + spec.DetonationDelayRandomExtra), Quaternion.FromToRotation(Vector3.forward, normal));
            }
            else if (spec.DespawnOnHit) Despawn();
            else StartCoroutine(DespawnAfter(RemoveAfterHit));
        }

        // 貫通: 命中を止まった位置とそこのコライダーに置き換える。判定できなければ通常の命中のまま
        private bool Penetrate(ref Collider hitCollider, ref Vector3 point)
        {
            float speed = _flightVelocity.magnitude;
            if (speed < 0.01f) return false;
            Vector3 stopPoint;
            Collider stopCollider;
            if (!Penetration.Resolve(point, _flightVelocity / speed, _spec.Penetration, _rb, CollisionMask(gameObject.layer), out stopPoint, out stopCollider))
                return false;
            hitCollider = stopCollider;
            point = stopPoint;
            return true;
        }

        private static int CollisionMask(int layer)
        {
            int mask = 0;
            for (int i = 0; i < 32; i++)
                if (!Physics.GetIgnoreLayerCollision(layer, i)) mask |= 1 << i;
            return mask;
        }

        private void AttachTo(Transform target)
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

        private bool Explodes
        {
            get
            {
                var explosion = _spec.Explosion;
                return explosion.Radius > 0f && (!explosion.OnlyWithExplodingCannonballs || _godToolExplosion);
            }
        }

        private void Detonate(float delay, Quaternion effectRotation)
        {
            _detonating = true;
            StartCoroutine(DetonateAfter(delay, effectRotation));
        }

        private IEnumerator DetonateAfter(float delay, Quaternion effectRotation)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            var explosion = _spec.Explosion;
            var pos = transform.position + transform.forward * explosion.ForwardOffset;
            if (Explodes)
            {
                if (_godToolExplosion) Simulation.MarkGodToolUsed();
                var affected = Weapon.HasHitListeners(_weapon) ? new List<Rigidbody>() : null;
                Damage.Explode(pos, explosion.Radius, explosion.Force, explosion.UpwardsModifier, affected, _shooter);
                if (affected != null)
                    foreach (var body in affected) RaiseHit(body.GetComponent<BlockBehaviour>(), null, pos, HitKind.Explosion);
                var rotation = explosion.FacesHitNormal ? effectRotation : Quaternion.identity;
                EffectPlayer.PlayOnce(explosion.Effect, null, pos, rotation, Vector3.one, _simulationScope, _spec.PoolSize);
                EffectPlayer.PlaySound(explosion.Sounds, pos, Sounds.ExplosionTemplate);
                if (NetworkId != 0) ProjectileSync.Despawned(this, true, pos, rotation);
                Despawn();
            }
            else
            {
                if (!_spec.DespawnOnHit) yield return new WaitForSeconds(RemoveAfterHit);
                Despawn();
            }
        }

        private IEnumerator DespawnAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            Despawn();
        }

        private void RaiseHit(BlockBehaviour block, Collider collider, Vector3 point, HitKind kind)
        {
            Weapon.RaiseHit(new ProjectileHit
            {
                Source = HitSource.Ewl,
                Weapon = _weapon,
                ShooterBlock = _shooterBlock,
                Shooter = _shooter,
                Block = block,
                Collider = collider,
                Point = point,
                Kind = kind,
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
            EffectPlayer.Return(_attachedEffect);
            _trail = _attachedEffect = null;
            if (_guidance != null) _guidance.End();
            SetIgnore(false);
            _ignored = null;
            _owner = null;
            _spec = null;
            _simulationScope = null;
            _weapon = null;
            _shooterBlock = null;
            _shooter = null;
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
