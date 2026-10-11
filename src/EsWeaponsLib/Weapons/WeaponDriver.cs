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
        internal Weapon Owner;
        internal WeaponSpec Spec;
        internal Action Fired;
        internal Action ReloadStarted;
        internal int Ammo;
        internal int ReserveAmmo;
        internal bool IsReloading;

        private BlockBehaviour _block;
        private Collider[] _ownColliders;
        private float _nextFireTime;
        private bool _bursting;
        private int _pendingShots; // 遅延待ちで、まだ弾が出ていない発射
        private GameObject _loadedVisual;
        private MeshFilter _loadedMeshFilter;
        private MeshRenderer _loadedRenderer;
        private PhysicMaterial _physicMaterial;
        private Material _material;
        private Texture _materialTexture;
        private string _privatePoolKey;
        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();

        internal BlockBehaviour Block { get { return _block; } }

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
            int capacity = spec.Fire.MagazineCapacity;
            Ammo = spec.Fire.Ammo;
            if (capacity > 0)
            {
                Ammo = Ammo < 0 ? capacity : Mathf.Min(capacity, spec.Fire.Ammo);
                ReserveAmmo = spec.Fire.Ammo < 0 ? -1 : spec.Fire.Ammo - Ammo;
            }
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
            bool show = Spec.Projectile.ShowWhenLoaded && !IsReloading && HasAmmo()
                && (_pendingShots > 0 || (Time.time >= _nextFireTime && !_bursting));
            if (show) UpdateLoadedVisual();
            if (_loadedVisual != null && _loadedVisual.activeSelf != show) _loadedVisual.SetActive(show);
        }

        // 発射位置に置く次の弾の見た目。Spec は書き換えられうるので表示中は毎フレーム合わせる(割り当てなし)。
        private void UpdateLoadedVisual()
        {
            var projectile = Spec.Projectile;
            if (_loadedVisual == null)
            {
                _loadedVisual = new GameObject("EwlLoadedProjectile");
                _loadedVisual.transform.SetParent(transform, false);
                var visual = new GameObject("Vis");
                visual.transform.SetParent(_loadedVisual.transform, false);
                _loadedMeshFilter = visual.AddComponent<MeshFilter>();
                _loadedRenderer = visual.AddComponent<MeshRenderer>();
                _owned.Add(_loadedVisual);
            }
            var root = _loadedVisual.transform;
            root.localPosition = Spec.Fire.MuzzlePosition;
            root.localRotation = Spec.Fire.MuzzleRotation;
            var visualTransform = _loadedMeshFilter.transform;
            if (projectile.Mesh != null)
            {
                visualTransform.localPosition = projectile.MeshPosition;
                visualTransform.localRotation = Quaternion.Euler(projectile.MeshRotation);
                visualTransform.localScale = projectile.MeshScale;
                _loadedMeshFilter.sharedMesh = projectile.Mesh;
            }
            else
            {
                visualTransform.localPosition = Vector3.zero;
                visualTransform.localRotation = Quaternion.identity;
                visualTransform.localScale = Vector3.one * (Projectile.DefaultRadius * 2f);
                _loadedMeshFilter.sharedMesh = Projectile.SphereMesh();
            }
            _loadedRenderer.sharedMaterial = MaterialFor(projectile);
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

        private bool UsesMagazine { get { return Spec.Fire.MagazineCapacity > 0; } }

        internal bool TryFire()
        {
            if (!Simulation.HasAuthority(_block) || Time.time < _nextFireTime || _bursting || IsReloading) return false;
            if (!HasAmmo())
            {
                if (UsesMagazine && Spec.Fire.AutoReload) TryReload();
                return false;
            }
            StartCoroutine(Burst());
            return true;
        }

        // 無限弾薬でも弾倉は減る(リロードの手間は残す)。
        private bool HasAmmo()
        {
            return Ammo != 0 || (!UsesMagazine && Simulation.IsInfiniteAmmo(_block));
        }

        private IEnumerator Burst()
        {
            _bursting = true;
            int count = Mathf.Max(1, Spec.Fire.BurstCount);
            for (int i = 0; i < count && HasAmmo() && !IsReloading; i++)
            {
                if (i > 0) yield return new WaitForSeconds(Spec.Fire.BurstInterval);
                if (_block == null || !HasAmmo()) break;
                ConsumeOne();
                var fire = Spec.Fire;
                StartCoroutine(Shoot(fire.Delay + UnityEngine.Random.Range(0f, fire.RandomDelay)));
            }
            _nextFireTime = Time.time + Spec.Fire.Interval;
            _bursting = false;
            if (Ammo == 0 && UsesMagazine && Spec.Fire.AutoReload) TryReload();
        }

        private void ConsumeOne()
        {
            bool infinite = Simulation.IsInfiniteAmmo(_block);
            if (Ammo > 0 && (UsesMagazine || !infinite)) Ammo--;
            if (Fired != null) Fired();
            ProjectileSync.WeaponStateChanged(this, WeaponStateFlags.Fired);
        }

        internal bool TryReload()
        {
            if (!Simulation.HasAuthority(_block) || !UsesMagazine || IsReloading || _bursting) return false;
            if (Ammo >= Spec.Fire.MagazineCapacity || (ReserveAmmo == 0 && !Simulation.IsInfiniteAmmo(_block))) return false;
            StartCoroutine(Reload());
            return true;
        }

        private IEnumerator Reload()
        {
            IsReloading = true;
            if (ReloadStarted != null) ReloadStarted();
            EffectPlayer.PlaySound(Spec.Effects.ReloadSounds, transform.position, Sounds.FireTemplate);
            ProjectileSync.WeaponStateChanged(this, WeaponStateFlags.ReloadStarted);
            yield return new WaitForSeconds(Spec.Fire.ReloadTime);
            int load = Mathf.Max(0, Spec.Fire.MagazineCapacity - Ammo);
            if (ReserveAmmo >= 0 && !Simulation.IsInfiniteAmmo(_block))
            {
                load = Mathf.Min(load, ReserveAmmo);
                ReserveAmmo -= load;
            }
            Ammo += load;
            IsReloading = false;
            ProjectileSync.WeaponStateChanged(this, WeaponStateFlags.None);
        }

        private IEnumerator Shoot(float delay)
        {
            _pendingShots++;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            yield return new WaitForFixedUpdate();
            _pendingShots--;
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
            var velocity = rb.velocity + dir * fire.Velocity;
            var shot = ProjectilePool.Acquire(PoolKey, projectile);
            shot.Launch(this, Spec, pos, rot, velocity, _ownColliders, physicMaterial, material);
            rb.AddForce(-dir * (fire.Velocity * projectile.Mass * fire.Recoil), ForceMode.Impulse);
            PlayShotEffects();
            ProjectileSync.Spawned(this, shot, pos, rot, velocity);
        }

        private void PlayShotEffects()
        {
            var effects = Spec.Effects;
            var blockTransform = transform;
            EffectPlayer.PlayOnce(effects.ShotFlash, blockTransform, effects.ShotFlashPosition, Quaternion.Euler(effects.ShotFlashRotation),
                effects.ShotFlashScale, Scope, Spec.Projectile.PoolSize);
            EffectPlayer.PlaySound(effects.FireSounds, blockTransform.TransformPoint(effects.ShotFlashPosition), Sounds.FireTemplate);
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
            proxy.LaunchProxy(this, Spec, position, rotation, velocity, MaterialFor(projectile));
            _nextFireTime = Time.time + Spec.Fire.Interval; // 待機弾の表示を発射間隔だけ消す
            PlayShotEffects();
            return proxy;
        }

        internal void ReceiveState(int ammo, int reserveAmmo, WeaponStateFlags flags)
        {
            Ammo = ammo;
            ReserveAmmo = reserveAmmo;
            IsReloading = (flags & WeaponStateFlags.Reloading) != 0;
            if ((flags & WeaponStateFlags.Fired) != 0 && Fired != null) Fired();
            if ((flags & WeaponStateFlags.ReloadStarted) != 0)
            {
                if (ReloadStarted != null) ReloadStarted();
                EffectPlayer.PlaySound(Spec.Effects.ReloadSounds, transform.position, Sounds.FireTemplate);
            }
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
