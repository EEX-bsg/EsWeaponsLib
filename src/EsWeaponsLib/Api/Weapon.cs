using System;
using EWL.Besiege;
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

        /// <summary>
        /// ビルド中のブロックに、装填中の弾の見た目(<see cref="WeaponSpec.ShowLoadedProjectile"/> と同じもの)を置く。見た目だけで当たり判定はない。
        /// 表示するかは呼び出し側が決める(Spec の ShowLoadedProjectile は見ない)。位置・弾の形はこの時の Spec で決まり、後から Spec を書き換えても追従しない。
        /// シミュ中は表示されない(シミュ側で見せるのは <see cref="WeaponSpec.ShowLoadedProjectile"/>)。ブロックと一緒に消え、途中で消すときは戻り値を Destroy する。
        /// </summary>
        public static GameObject CreateLoadedProjectilePreview(BlockBehaviour block, WeaponSpec spec)
        {
            if (block == null) throw new ArgumentNullException("block");
            if (spec == null) throw new ArgumentNullException("spec");
            var preview = LoadedProjectileVisual.Create(block.transform, true);
            preview.Apply(spec);
            return preview.gameObject;
        }

        /// <summary>定義。発射のたびに読まれるので、書き換えると次の弾から反映される。
        /// ただし総弾数(TotalAmmo)と弾倉の初期の詰め方は <see cref="Create"/> の時だけ使う。</summary>
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

        /// <summary>
        /// この武装が使う弾のプールのキー。<see cref="ProjectileSpec.PoolKey"/> が空ならこの武装専用のキー。破棄済みなら null。
        /// プールの置き場はシーン直下の "EsWeaponsLib (Scene)/ProjectilePool/(キー)" で、待機中・飛行中の弾はその子(命中物にくっついた弾はその命中物の子)。
        /// 弾に直接触るのは推奨しない(足りない操作は API への追加を提案してほしい)。置き場の構成は 1.0 まで変わりうる。
        /// </summary>
        public string PoolKey { get { return _driver != null ? _driver.PoolKey : null; } }

        /// <summary>
        /// 誘導の目標(敵マシンのコアブロックの Transform。<see cref="FindTarget"/> で探す)。<see cref="TryFire"/> が発射を受け付けた時点の値で、
        /// その発射で出る弾(遅延・バーストで後から出る弾を含む)の目標が確定する。確定した後に変えても、発射待ち・飛行中の弾には効かない。
        /// null、または発射の受付時に有効な敵のコアでない(味方・壊れた・リスポーンした・任意の物体等)なら、その発射の弾は無誘導で飛ぶ。
        /// <see cref="GuidanceSpec.Gain"/> が0の武装では使われない。
        /// MP: 撃つブロックの持ち主のピアで設定する。持ち主がクライアント(グローバルシミュ)なら実行権威のホストへ送られ、
        /// ホストは持ち主以外からの指定を受け付けず、敵のコアでない指定は null として扱う。ホスト側でクライアントのブロックの目標を決めないこと
        /// (ホストのカメラはそのクライアントの照準ではない)。読むと、このピアで最後に設定・受信した値。
        /// </summary>
        public Transform Target
        {
            get { return _driver != null ? _driver.Target : null; }
            set { if (_driver != null) _driver.SetTarget(value); }
        }

        /// <summary>
        /// 照準(位置 <c>aimOrigin</c> と向き <c>aimDirection</c>)から <c>maxAngle</c> [度] 以内にいる敵マシンのコアブロック(の Transform)のうち、
        /// 照準の向きとのなす角が最も小さいものを返す(距離では選ばない)。無ければ null。
        /// 敵 = この武装のブロックのマシン以外で、観戦中でなく、グローバルシミュ中のプレイヤーのマシン。ブロックにチームがあれば同じチームは除く(チーム無しは全員が敵)。
        /// SP・ローカルシミュには他人がいないので null(コンソールコマンド ewl_dummy で置いたテスト用のダミー標的は、このピアでだけ敵として選ばれる)。
        /// どのピアでも呼べ、毎フレーム呼んでよい。目標の指定は <see cref="Target"/> で行う。
        /// </summary>
        public Transform FindTarget(Vector3 aimOrigin, Vector3 aimDirection, float maxAngle)
        {
            return _driver != null ? MachineTargets.FindNearestToAim(_driver.Block, aimOrigin, aimDirection, maxAngle) : null;
        }

        /// <summary>破棄済みなら false。</summary>
        public bool IsAlive { get { return _driver != null; } }

        /// <summary>1発撃つ(弾数を消費する)たび。バーストでは弾ごと。弾の生成は遅延の後。</summary>
        public event Action Fired;

        /// <summary>リロードを始めたとき。弾倉はリロード時間の後に満たされる。</summary>
        public event Action ReloadStarted;

        /// <summary>この武装の弾が何かに当たった(直撃・爆風)。実行権威側(SP・MP ホスト・ローカルシミュ)でだけ発火する。</summary>
        public event Action<ProjectileHit> Hit;

        /// <summary>
        /// 弾が当たるたびに、撃った武装を問わず発火する。被弾した側が撃った相手を知るために使う。
        /// 対象は EWL の弾(EWL を使う他 Mod の武装を含む)と、一部のバニラ武装の弾(大砲・クロスボウ・FuelCannon。ゴッドツール「爆発する砲弾」の爆風を含む)。
        /// どれかは <see cref="ProjectileHit.Source"/> で分かる。それ以外のバニラ武装(散弾砲・火炎放射器等)は今後対応することがある。他の仕組みの Mod の弾は対象外。
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
