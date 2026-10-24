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
        private const float PassThroughGap = 0.02f;     // 貫通して置き直した弾と、出口・次に当てる装甲の間
        private const float RemoveAfterHit = 1f;         // 爆発も即消滅もしない弾が命中後に消えるまで(ACM と同じ)
        private const float MaxExtrapolation = 0.5f;     // プロキシ: スナップショットが途絶えたら外挿を止める
        private const float CorrectionRate = 10f;        // プロキシ: スナップショットとのずれを詰める速さ [1/s]
        internal const float DefaultRadius = 0.1f;

        private static Mesh _sphereMesh;
        private static readonly Collider[] OverlapBuffer = new Collider[64];

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
        private readonly List<Collider> _relocationIgnored = new List<Collider>(); // 貫通で置き直した所で重なり、衝突しないようにしたブロック
        private PooledEffect _trail, _attachedEffect;
        private MissileGuidance _guidance; // 誘導する弾だけ。弾のオブジェクトと一緒に使い回す
        private float _guidanceGain; // 発射時の値。0 なら誘導しない
        private float _age;
        private float _penetrationLeft; // 残りの貫通量 [m]。装甲を抜けた分だけ減る
        private float _passThroughTime; // 貫通して置き直した物理フレームの Time.fixedTime(同じフレームの残りの接触を無視する)
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

        // 1物理フレーム前(この弾の FixedUpdate の時点)の状態。衝突の通知の中で読むと衝突前の値になる(_rb.velocity 等は衝突後)
        internal Vector3 PreviousVelocity { get; private set; }
        internal Vector3 PreviousAngularVelocity { get; private set; }
        internal Quaternion PreviousRotation { get; private set; }

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
            PreviousVelocity = velocity;
            PreviousAngularVelocity = Vector3.zero;
            PreviousRotation = rot;
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
            _penetrationLeft = spec.Penetration;
            _passThroughTime = -1f;
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
            if (!_active || _proxy) return;
            PreviousVelocity = _rb.velocity;
            PreviousAngularVelocity = _rb.angularVelocity;
            PreviousRotation = _rb.rotation;
            if (_hit || _detonating) return;
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
        }

        private static bool BelowMaxSpeed(BoosterSpec booster, Vector3 velocity)
        {
            return booster.MaxSpeed <= 0f || velocity.sqrMagnitude < booster.MaxSpeed * booster.MaxSpeed;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hit || _detonating) return;
            if (Time.fixedTime == _passThroughTime) return; // 装甲を抜けたのと同じ物理フレームの別の接触(抜けた装甲の別のコライダー等)
            var other = collision.collider.GetComponentInParent<Projectile>();
            // 同じ武装の弾同士は無視。参照で比べる(武装が破棄済みだと Unity の == は別々の武装でも一致する)
            if (other != null && ReferenceEquals(other._owner, _owner)) return;

            var hitCollider = collision.collider;
            var point = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            var normal = collision.contacts.Length > 0 ? collision.contacts[0].normal : -transform.forward;
            // 貫通するのはマシンのブロックだけ。地形・レベルの物体には普通に当たる(物理の衝突で跳ねる)
            if (_penetrationLeft > 0f && Damage.BlockOf(hitCollider) != null && Penetrate(hitCollider, point, normal))
                return; // 貫通して飛び続ける
            HitDirectly(hitCollider, point);
            EndFlight(normal);
        }

        // 普通の命中: 当たった物にダメージ・衝撃力を与える。弾は物理の衝突のまま跳ねる(AttachOnHit なら刺さる)
        private void HitDirectly(Collider hitCollider, Vector3 point)
        {
            var spec = _spec;
            _hit = true;
            var attachTo = Damage.ApplyHit(hitCollider, PreviousVelocity, spec.ImpactForce, spec.BlockDamage, spec.EntityDamage, spec.AttachOnHit, _shooter);
            if (Weapon.HasHitListeners(_weapon))
                RaiseHit(Damage.BlockOf(hitCollider), hitCollider, point, HitKind.Direct);
            if (spec.AttachOnHit) AttachTo(attachTo);
        }

        // 命中で飛行を終える: 起爆するか、消える
        private void EndFlight(Vector3 normal)
        {
            EffectPlayer.Fade(_trail);
            _trail = null;
            if (Explodes) Detonate(DetonationDelay(), Quaternion.FromToRotation(Vector3.forward, normal));
            else if (_spec.DespawnOnHit) Despawn();
            else StartCoroutine(DespawnAfter(RemoveAfterHit));
        }

        private float DetonationDelay()
        {
            return Random.Range(_spec.DetonationDelay, _spec.DetonationDelay + _spec.DetonationDelayRandomExtra);
        }

        // 貫通: 抜けたブロックにダメージを与え、弾を置き直して衝突前の速度・向きで飛び続けさせる。装甲の中では止めない。
        //  抜けた: 後端が出口の外に出る所へ。爆発する弾はここで起爆を始める
        //  FirstBlock で抜けたが、裏のブロックが近く弾が入らない: 前端が裏面の手前に来る所へ。爆発する弾はここで起爆を始める(抜けたのと同じ)
        //  Total で手前のブロックを抜け、奥のブロックで尽きた: 前端がそのブロックの入口の手前に来る所へ。そのブロックには普通の弾として当たる
        // 判定できない・当たったブロック自体を抜けられない(貫通失敗)なら false(貫通せず普通の命中として扱う)
        private bool Penetrate(Collider hit, Vector3 contact, Vector3 normal)
        {
            float speed = PreviousVelocity.magnitude;
            if (speed < 0.01f) return false;
            var direction = PreviousVelocity / speed;
            float rear, front;
            ExtentAlong(direction, out rear, out front);
            bool firstBlock = _spec.PenetrationMode == PenetrationMode.FirstBlock;
            Vector3 stopPoint;
            Collider stopCollider, blocker;
            bool exited, embedded;
            float used;
            if (!Penetration.Resolve(contact, direction, speed * Time.fixedDeltaTime, _penetrationLeft, rear + front + PassThroughGap, hit,
                    firstBlock, _rb, CollisionMask(gameObject.layer), out stopPoint, out stopCollider, out exited, out embedded, out blocker, out used))
                return false;
            int failed = embedded ? Penetration.PassedCount - 1 : -1; // 抜けられなかったブロック(通ったブロックの最後)
            if (failed == 0) return false;
            // FirstBlock は1ブロックで貫通を終え、抜けた後は普通の弾として当たる。Total は残りの貫通量で次の装甲も貫通する
            _penetrationLeft = firstBlock || embedded ? 0f : _penetrationLeft - used;
            HitPassedBlocks(embedded ? failed : Penetration.PassedCount);
            if (embedded)
            {
                var failedCollider = Penetration.Passed(failed);
                Relocate(Penetration.PassedPoint(failed) - direction * (front + PassThroughGap), failedCollider != null ? failedCollider.attachedRigidbody : null);
                return true;
            }
            if (exited) Relocate(stopPoint + direction * (rear + PassThroughGap), null);
            else Relocate(stopPoint - direction * (front + PassThroughGap), blocker != null ? blocker.attachedRigidbody : null);
            if (Explodes) Detonate(DetonationDelay(), Quaternion.FromToRotation(Vector3.forward, normal));
            return true;
        }

        // 通ったブロックのうち先頭から count 個それぞれに、1回ずつダメージ・衝撃力・命中イベントを与える
        private void HitPassedBlocks(int count)
        {
            var spec = _spec;
            bool raise = Weapon.HasHitListeners(_weapon);
            for (int i = 0; i < count; i++)
            {
                var collider = Penetration.Passed(i);
                if (collider == null) continue; // 先のブロックの処理で壊れた
                Damage.ApplyHit(collider, PreviousVelocity, spec.ImpactForce, spec.BlockDamage, spec.EntityDamage, spec.AttachOnHit, _shooter);
                if (raise) RaiseHit(Damage.BlockOf(collider), collider, Penetration.PassedPoint(i), HitKind.Direct);
            }
        }

        // 弾のコライダーが、弾の位置から direction の後ろ・前へどれだけ出ているか [m]。
        // コライダーが弾の位置より後ろだけにあれば front は負(rear + front がコライダーの長さ)
        private void ExtentAlong(Vector3 direction, out float rear, out float front)
        {
            rear = front = float.NegativeInfinity;
            var position = transform.position;
            foreach (var collider in _colliders)
            {
                var bounds = collider.bounds;
                var extents = bounds.extents;
                float extent = Mathf.Abs(direction.x) * extents.x + Mathf.Abs(direction.y) * extents.y + Mathf.Abs(direction.z) * extents.z;
                float offset = Vector3.Dot(bounds.center - position, direction);
                rear = Mathf.Max(rear, extent - offset);
                front = Mathf.Max(front, extent + offset);
            }
        }

        // 貫通した弾を置き直し、衝突前の速度・向きで飛び続けさせる。置いた所で重なるマシンのブロックとは衝突しない
        // (通ってきた装甲に穴が開いた扱い。Despawn で戻す)。next: 次に当てるブロック(重なっていても衝突を残す)
        private void Relocate(Vector3 position, Rigidbody next)
        {
            _passThroughTime = Time.fixedTime;
            transform.rotation = PreviousRotation;
            transform.position = position;
            _rb.velocity = PreviousVelocity;
            _rb.angularVelocity = PreviousAngularVelocity;
            int mask = CollisionMask(gameObject.layer);
            foreach (var collider in _colliders)
            {
                int count = Overlap(collider, mask);
                for (int i = 0; i < count; i++)
                {
                    var other = OverlapBuffer[i];
                    var body = other.attachedRigidbody;
                    if (body == null || body == _rb || body == next || body.GetComponent<BlockBehaviour>() == null) continue;
                    if (_relocationIgnored.Contains(other)) continue;
                    _relocationIgnored.Add(other);
                    foreach (var own in _colliders) Physics.IgnoreCollision(own, other, true);
                }
            }
        }

        private void RestoreRelocationIgnores()
        {
            foreach (var other in _relocationIgnored)
                if (other != null) // 壊れて破棄済みのことがある
                    foreach (var own in _colliders) Physics.IgnoreCollision(own, other, false);
            _relocationIgnored.Clear();
        }

        // 弾のコライダーの今の位置・形で重なるコライダーを OverlapBuffer に集める
        private static int Overlap(Collider collider, int mask)
        {
            var t = collider.transform;
            var scale = t.lossyScale;
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            var box = collider as BoxCollider;
            if (box != null)
                return Physics.OverlapBoxNonAlloc(t.TransformPoint(box.center), Vector3.Scale(box.size, scale) * 0.5f, OverlapBuffer, t.rotation,
                    mask, QueryTriggerInteraction.Ignore);
            var sphere = collider as SphereCollider;
            if (sphere != null)
                return Physics.OverlapSphereNonAlloc(t.TransformPoint(sphere.center), sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)),
                    OverlapBuffer, mask, QueryTriggerInteraction.Ignore);
            var capsule = collider as CapsuleCollider;
            if (capsule != null)
            {
                int axis = capsule.direction;
                float radius = capsule.radius * (axis == 0 ? Mathf.Max(scale.y, scale.z) : axis == 1 ? Mathf.Max(scale.x, scale.z) : Mathf.Max(scale.x, scale.y));
                float half = Mathf.Max(capsule.height * scale[axis] * 0.5f - radius, 0f);
                var center = t.TransformPoint(capsule.center);
                var up = t.rotation * (axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward);
                return Physics.OverlapCapsuleNonAlloc(center + up * half, center - up * half, radius, OverlapBuffer, mask, QueryTriggerInteraction.Ignore);
            }
            var bounds = collider.bounds; // 弾が持つのは上の3種類だけ
            return Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, OverlapBuffer, Quaternion.identity, mask, QueryTriggerInteraction.Ignore);
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
            RestoreRelocationIgnores(); // 使い回した弾が他のブロックを素通りしないよう、最初に戻す
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
