using System;
using EWL.Weapons;
using UnityEngine;

namespace EWL.Api
{
    /// <summary>
    /// 武装部品。利用側(BlockModuleBehaviour 等)がシミュ開始で <see cref="Create"/> し、シミュ停止で <see cref="Destroy"/> する。
    /// いつ撃つかは利用側が決めて <see cref="TryFire"/> を呼ぶ。弾体・着弾・爆発・エフェクトは EWL が行う。
    /// </summary>
    public sealed class Weapon
    {
        private readonly WeaponDriver _driver;

        private Weapon(WeaponDriver driver) { _driver = driver; }

        /// <summary>ブロックに武装を付ける。1ブロックに複数付けてよい。</summary>
        public static Weapon Create(BlockBehaviour block, WeaponSpec spec)
        {
            if (block == null) throw new ArgumentNullException("block");
            if (spec == null) throw new ArgumentNullException("spec");
            var driver = block.gameObject.AddComponent<WeaponDriver>();
            var weapon = new Weapon(driver);
            driver.Owner = weapon;
            driver.Fired = weapon.RaiseFired;
            driver.ReloadStarted = weapon.RaiseReloadStarted;
            driver.Init(block, spec);
            return weapon;
        }

        /// <summary>ブロックに付いている EWL の武装(他 Mod が付けたものを含む)。</summary>
        public static Weapon[] GetAll(GameObject block)
        {
            var drivers = block.GetComponents<WeaponDriver>();
            var result = new Weapon[drivers.Length];
            for (int i = 0; i < drivers.Length; i++) result[i] = drivers[i].Owner;
            return result;
        }

        /// <summary>定義。発射のたびに読まれるので、書き換えると次の弾から反映される。
        /// ただし弾数(Fire.Ammo)と弾倉の初期の詰め方は <see cref="Create"/> の時だけ使う。</summary>
        public WeaponSpec Spec
        {
            get { return _driver.Spec; }
            set { if (value == null) throw new ArgumentNullException("value"); _driver.Spec = value; }
        }

        /// <summary>すぐ撃てる残弾(弾倉を使う武装は弾倉内の弾)。負値は無制限。</summary>
        public int Ammo { get { return _driver.Ammo; } }

        /// <summary>弾倉の外の予備弾。負値は無制限。弾倉を使わない武装は 0。</summary>
        public int ReserveAmmo { get { return _driver.ReserveAmmo; } }

        /// <summary>リロード中なら true(撃てない)。</summary>
        public bool IsReloading { get { return _driver.IsReloading; } }

        /// <summary>破棄済みなら false。</summary>
        public bool IsAlive { get { return _driver != null; } }

        /// <summary>1発撃つ(弾数を消費する)たび。バーストでは弾ごと。弾の生成は遅延の後。</summary>
        public event Action Fired;

        /// <summary>リロードを始めたとき。弾倉はリロード時間の後に満たされる。</summary>
        public event Action ReloadStarted;

        /// <summary>この武装の弾が何かに当たった(直撃・爆風)。実行権威側(SP・MP ホスト・ローカルシミュ)でだけ発火する。</summary>
        public event Action<ProjectileHit> Hit;

        /// <summary>
        /// どの武装の弾が当たっても発火する(他 Mod の武装を含む)。被弾した側が撃った相手を知るために使う。
        /// 実行権威側でだけ発火する。静的イベントなので、購読したら必ず解除する。
        /// </summary>
        public static event Action<ProjectileHit> AnyHit;

        /// <summary>
        /// 発射を要求する。実行権威側(SP・MP Host・MP Client ローカルシミュ)で、間隔・弾数・リロード中でないことが許すときだけ受け付けて true。
        /// BurstCount が 2 以上なら、この1回でバーストを撃つ。
        /// </summary>
        public bool TryFire()
        {
            return _driver != null && _driver.TryFire();
        }

        /// <summary>
        /// リロードを要求する。実行権威側で、弾倉を使い、弾倉に空きがあり、予備弾があり、リロード中でないときだけ受け付けて true。
        /// </summary>
        public bool TryReload()
        {
            return _driver != null && _driver.TryReload();
        }

        /// <summary>武装を外す。飛んでいる弾はシミュ終了まで残る。</summary>
        public void Destroy()
        {
            if (_driver != null) UnityEngine.Object.Destroy(_driver);
        }

        private void RaiseFired() { Raise(Fired, "Fired"); }

        private void RaiseReloadStarted() { Raise(ReloadStarted, "ReloadStarted"); }

        internal static bool HasHitListeners(Weapon weapon)
        {
            return AnyHit != null || (weapon != null && weapon.Hit != null);
        }

        internal static void RaiseHit(ProjectileHit hit)
        {
            if (hit.Weapon != null) Raise(hit.Weapon.Hit, hit, "Hit");
            Raise(AnyHit, hit, "AnyHit");
        }

        private static void Raise(Action handler, string eventName)
        {
            if (handler == null) return;
            // 利用側ハンドラの例外で発射・リロード処理を止めない
            try { handler(); }
            catch (Exception ex) { Debug.LogError("[EWL] Weapon." + eventName + " handler threw: " + ex); }
        }

        private static void Raise(Action<ProjectileHit> handler, ProjectileHit hit, string eventName)
        {
            if (handler == null) return;
            try { handler(hit); }
            catch (Exception ex) { Debug.LogError("[EWL] Weapon." + eventName + " handler threw: " + ex); }
        }
    }
}
