using System;
using System.Collections;
using System.Collections.Generic;
using EWL.Api;
using EWL.Besiege;
using EWL.Effects;
using EWL.Net;
using UnityEngine;

namespace EWL.Weapons
{
    // 武装1基の実体。ブロックの GameObject に付く。公開面は Api.Weapon が包む。
    internal sealed class WeaponDriver : MonoBehaviour
    {
        private const float TargetResendInterval = 1f; // MP クライアントの持ち主が目標を送り直す間隔 [s]

        internal Weapon Owner;
        internal WeaponSpec Spec;
        internal Action Fired;
        internal Action ReloadStarted;
        internal int Ammo;
        internal int ReserveAmmo;
        internal bool IsReloading;
        /// <summary>誘導の目標の指定(Weapon.Target)。発射の受付時に確かめて確定する。</summary>
        internal Transform Target;

        private BlockBehaviour _block;
        private Collider[] _ownColliders;
        private float _nextFireTime;
        private bool _bursting;
        private int _pendingShots; // 遅延待ちで、まだ弾が出ていない発射
        private float _proxyPendingUntil; // MP クライアント: 発射が届いてから弾のプロキシが出るまで(取りこぼしても遅延の上限で切れる)
        private float _nextTargetResendTime;
        private LoadedProjectileVisual _loadedVisual;
        private string _privatePoolKey;
        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();

        internal BlockBehaviour Block { get { return _block; } }

        internal float FireCooldownRemaining { get { return Mathf.Max(0f, _nextFireTime - Time.time); } }

        /// <summary>ブロック上で何番目に作られた武装か。MP で全ピアの武装を対応付ける(全ピアで同じ順に生成される契約)。</summary>
        internal int NetworkIndex { get; private set; }

        internal static WeaponDriver Find(BlockBehaviour block, int networkIndex)
        {
            if (block == null) return null;
            foreach (var driver in block.GetComponents<WeaponDriver>())
                if (driver.NetworkIndex == networkIndex) return driver;
            return null;
        }

        internal void Init(BlockBehaviour block, WeaponSpec spec)
        {
            _block = block;
            Spec = spec;
            // 途中で破棄された武装があっても重複しないよう、既存の最大 + 1
            int index = 0;
            foreach (var other in GetComponents<WeaponDriver>())
                if (other != this) index = Mathf.Max(index, other.NetworkIndex + 1);
            NetworkIndex = index;
            int capacity = spec.MagazineCapacity;
            Ammo = spec.TotalAmmo;
            if (capacity > 0)
            {
                Ammo = Ammo < 0 ? capacity : Mathf.Min(capacity, spec.TotalAmmo);
                ReserveAmmo = spec.TotalAmmo < 0 ? -1 : spec.TotalAmmo - Ammo;
            }
            float carried = spec.KeepFireIntervalOnRespawn ? FireCooldowns.Restore(block, NetworkIndex) : 0f;
            _nextFireTime = Mathf.Max(carried, Time.time + spec.StartDelay);
            _ownColliders = block.GetComponentsInChildren<Collider>();
            _privatePoolKey = "weapon#" + GetInstanceID();
            ProjectilePool.Prewarm(PoolKey, spec.Projectile);
        }

        // 無効化でコルーチンが止まるので、途中のバースト・リロードを取り消す(フラグが残ると撃てなくなる)
        private void OnDisable()
        {
            _bursting = false;
            IsReloading = false;
            _pendingShots = 0;
        }

        private void Update()
        {
            bool show = Spec.ShowLoadedProjectile && !IsReloading && HasAmmo()
                && (_pendingShots > 0 || Time.time < _proxyPendingUntil || (Time.time >= _nextFireTime && !_bursting));
            if (show)
            {
                if (_loadedVisual == null)
                {
                    _loadedVisual = LoadedProjectileVisual.Create(transform, false);
                    _owned.Add(_loadedVisual.gameObject);
                }
                _loadedVisual.Apply(Spec); // Spec は書き換えられうるので表示中は毎フレーム合わせる
            }
            if (_loadedVisual != null && _loadedVisual.gameObject.activeSelf != show) _loadedVisual.gameObject.SetActive(show);
            // MP クライアントの持ち主: ホストの武装がまだ無い間に送った指定は捨てられるので、指定中は時々送り直す
            if (Target != null && Time.time >= _nextTargetResendTime) SendTarget();
        }

        private void OnDestroy()
        {
            if (_privatePoolKey != null) ProjectilePool.Discard(_privatePoolKey);
            foreach (var obj in _owned) Destroy(obj);
            _owned.Clear();
        }

        /// <summary>PoolKey 未指定ならこの武装専用のプール。</summary>
        internal string PoolKey
        {
            get
            {
                var key = Spec.Projectile.PoolKey;
                return string.IsNullOrEmpty(key) ? _privatePoolKey : key;
            }
        }

        private bool UsesMagazine { get { return Spec.MagazineCapacity > 0; } }

        internal void SetTarget(Transform target)
        {
            if (target == Target) return;
            Target = target;
            SendTarget();
        }

        private void SendTarget()
        {
            _nextTargetResendTime = Time.time + TargetResendInterval;
            ProjectileSync.TargetChanged(this);
        }

        /// <summary>ホスト: 持ち主のクライアントからの指定。有効な目標(敵のコア)でなければ指定なしにする。</summary>
        internal void ReceiveTarget(Transform target)
        {
            Target = MachineTargets.IsValidTarget(_block, target) ? target : null;
        }

        internal bool TryFire()
        {
            if (!Simulation.HasAuthority(_block) || Time.time < _nextFireTime || _bursting || IsReloading) return false;
            if (!HasAmmo())
            {
                if (UsesMagazine && Spec.AutoReload) TryReload();
                return false;
            }
            // 目標はこの発射要求の時点で確定する。遅延・バーストで後から出る弾も同じ目標を追い、その間に指定が変わっても変えない
            var target = MachineTargets.IsValidTarget(_block, Target) ? Target : null;
            StartCoroutine(Burst(target));
            return true;
        }

        // 無限弾薬でも弾倉は減る(リロードの手間は残す)。
        private bool HasAmmo()
        {
            return Ammo != 0 || (!UsesMagazine && Simulation.IsInfiniteAmmo(_block));
        }

        private IEnumerator Burst(Transform target)
        {
            _bursting = true;
            int count = Mathf.Max(1, Spec.BurstCount);
            for (int i = 0; i < count && HasAmmo() && !IsReloading; i++)
            {
                if (i > 0) yield return new WaitForSeconds(Spec.BurstInterval);
                if (_block == null || !HasAmmo()) break;
                ConsumeOne();
                StartCoroutine(Shoot(Spec.FireDelay + UnityEngine.Random.Range(0f, Spec.FireDelayRandomExtra), target));
            }
            _nextFireTime = Time.time + Spec.FireInterval;
            if (Spec.KeepFireIntervalOnRespawn) FireCooldowns.Record(_block, NetworkIndex, _nextFireTime);
            _bursting = false;
            if (Ammo == 0 && UsesMagazine && Spec.AutoReload) TryReload();
        }

        private void ConsumeOne()
        {
            bool infinite = Simulation.IsInfiniteAmmo(_block);
            if (infinite) Simulation.MarkGodToolUsed();
            if (Ammo > 0 && (UsesMagazine || !infinite)) Ammo--;
            if (Fired != null) Fired();
            ProjectileSync.WeaponStateChanged(this, WeaponStateFlags.Fired);
        }

        internal bool TryReload()
        {
            if (!Simulation.HasAuthority(_block) || !UsesMagazine || IsReloading || _bursting) return false;
            if (Ammo >= Spec.MagazineCapacity || (ReserveAmmo == 0 && !Simulation.IsInfiniteAmmo(_block))) return false;
            StartCoroutine(Reload());
            return true;
        }

        private IEnumerator Reload()
        {
            IsReloading = true;
            if (ReloadStarted != null) ReloadStarted();
            EffectPlayer.PlaySound(Spec.ReloadSounds, transform.position, Sounds.FireTemplate);
            ProjectileSync.WeaponStateChanged(this, WeaponStateFlags.ReloadStarted);
            yield return new WaitForSeconds(Spec.ReloadTime);
            int load = Mathf.Max(0, Spec.MagazineCapacity - Ammo);
            if (ReserveAmmo >= 0 && !Simulation.IsInfiniteAmmo(_block))
            {
                load = Mathf.Min(load, ReserveAmmo);
                ReserveAmmo -= load;
            }
            Ammo += load;
            IsReloading = false;
            ProjectileSync.WeaponStateChanged(this, WeaponStateFlags.None);
        }

        private IEnumerator Shoot(float delay, Transform target)
        {
            _pendingShots++;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            yield return new WaitForFixedUpdate();
            _pendingShots--;
            if (_block == null || !Simulation.HasAuthority(_block) || _block.Rigidbody == null) yield break;

            var spec = Spec;
            var projectile = spec.Projectile;
            var blockTransform = transform;
            Vector3 pos = blockTransform.TransformPoint(spec.MuzzlePosition);
            Quaternion rot = blockTransform.rotation * Quaternion.Euler(spec.MuzzleRotation);
            Vector3 dir = rot * Vector3.forward;
            if (spec.Spread > 0f)
            {
                dir = (dir + UnityEngine.Random.insideUnitSphere * Mathf.Tan(spec.Spread * Mathf.Deg2Rad)).normalized;
                rot = Quaternion.LookRotation(dir, rot * Vector3.up); // 推進する弾は向きの方へ飛ぶ
            }

            var rb = _block.Rigidbody;
            var physicMaterial = PhysicMaterialFor(projectile);
            var material = MaterialFor(projectile);
            var velocity = rb.velocity + dir * spec.MuzzleVelocity + rot * spec.EjectVelocity;
            var shot = ProjectilePool.Acquire(PoolKey, projectile);
            // 待つ間に目標が壊れていれば無誘導(別の目標へは替えない)
            shot.Launch(this, projectile, pos, rot, velocity, _ownColliders, physicMaterial, material, target);
            rb.AddForce(-dir * (spec.MuzzleVelocity * projectile.Mass * spec.RecoilMultiplier), ForceMode.Impulse);
            PlayShotEffects();
            ProjectileSync.Spawned(this, shot, pos, rot, velocity);
        }

        private void PlayShotEffects()
        {
            var spec = Spec;
            var blockTransform = transform;
            EffectPlayer.PlayOnce(spec.MuzzleFlash, blockTransform, spec.MuzzleFlashPosition, Quaternion.Euler(spec.MuzzleFlashRotation),
                spec.MuzzleFlashScale, Scope, spec.Projectile.PoolSize);
            EffectPlayer.PlaySound(spec.FireSounds, blockTransform.TransformPoint(spec.MuzzleFlashPosition), Sounds.FireTemplate);
        }

        /// <summary>エフェクト・弾の持ち主(これが消えたら回収)。シミュの目印が無いピアではこのブロック。</summary>
        internal UnityEngine.Object Scope
        {
            get
            {
                var scope = Simulation.Scope;
                return scope != null ? scope : gameObject;
            }
        }

        // ---- MP クライアント(ホストの弾の見た目だけを出す) ----

        internal Projectile SpawnProxy(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            var projectile = Spec.Projectile;
            var proxy = ProjectilePool.Acquire(PoolKey, projectile);
            proxy.LaunchProxy(this, projectile, position, rotation, velocity, MaterialFor(projectile));
            _proxyPendingUntil = 0f; // 弾が出たので待機弾を消す(発射間隔の目安は ReceiveState で数えている)
            PlayShotEffects();
            return proxy;
        }

        internal void ReceiveState(int ammo, int reserveAmmo, WeaponStateFlags flags)
        {
            Ammo = ammo;
            ReserveAmmo = reserveAmmo;
            IsReloading = (flags & WeaponStateFlags.Reloading) != 0;
            if ((flags & WeaponStateFlags.Fired) != 0)
            {
                // 発射間隔の目安。ホストはバーストの最後の弾から数えるので、届くたびに上書きすれば揃う(ずれは遅延ぶん遅れる向き)
                float now = Time.time;
                _nextFireTime = now + Spec.FireInterval;
                _proxyPendingUntil = now + Spec.FireDelay + Spec.FireDelayRandomExtra + Time.fixedDeltaTime;
                if (Spec.KeepFireIntervalOnRespawn) FireCooldowns.RecordEstimate(_block, NetworkIndex, _nextFireTime);
                if (Fired != null) Fired();
            }
            if ((flags & WeaponStateFlags.ReloadStarted) != 0)
            {
                if (ReloadStarted != null) ReloadStarted();
                EffectPlayer.PlaySound(Spec.ReloadSounds, transform.position, Sounds.FireTemplate);
            }
        }

        // 発射ごとに Spec を読む。武装が消えても飛行中の弾が使い続けるので共有品を使う。
        private static PhysicMaterial PhysicMaterialFor(ProjectileSpec projectile)
        {
            return ProjectileMaterials.For(projectile.Friction, projectile.FrictionCombine, projectile.Bounciness, projectile.BounceCombine);
        }

        private static Material MaterialFor(ProjectileSpec projectile)
        {
            return ProjectileMaterials.For(projectile.Texture);
        }
    }
}
