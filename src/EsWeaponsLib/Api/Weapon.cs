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

		/// <summary>定義。発射のたびに読まれるので、書き換えると次の弾から反映される。</summary>
		public WeaponSpec Spec
		{
			get { return _driver.Spec; }
			set { if (value == null) throw new ArgumentNullException("value"); _driver.Spec = value; }
		}

		/// <summary>残弾。負値は無制限。</summary>
		public int Ammo { get { return _driver.Ammo; } }

		/// <summary>破棄済みなら false。</summary>
		public bool IsAlive { get { return _driver != null; } }

		/// <summary>発射を受け付けた(弾数を消費した)とき。弾の生成は遅延の後。</summary>
		public event Action Fired;

		/// <summary>
		/// 発射を要求する。実行権威側(SP・MP Host・MP Client ローカルシミュ)で、間隔と弾数が許すときだけ受け付けて true。
		/// </summary>
		public bool TryFire()
		{
			return _driver != null && _driver.TryFire();
		}

		/// <summary>武装を外す。飛んでいる弾はシミュ終了まで残る。</summary>
		public void Destroy()
		{
			if (_driver != null) UnityEngine.Object.Destroy(_driver);
		}

		private void RaiseFired()
		{
			var handler = Fired;
			if (handler == null) return;
			// 利用側ハンドラの例外で発射処理を止めない
			try { handler(); }
			catch (Exception ex) { Debug.LogError("[EWL] Weapon.Fired handler threw: " + ex); }
		}
	}
}
