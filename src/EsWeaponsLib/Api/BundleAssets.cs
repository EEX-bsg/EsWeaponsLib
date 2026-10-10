using System.Collections.Generic;
using Modding;
using UnityEngine;

namespace EWL.Api
{
	/// <summary>
	/// <see cref="ModAssetBundle"/> からアセットを取り出す。本体の <c>LoadAsset</c> は呼ぶたびにバンドルの全アセットを読む(空バンドル検出)ので、
	/// 結果をバンドル・名前・型ごとに覚える。見つからなかった名前も覚え、2回目以降は読まない。
	/// <para>
	/// 本体はバンドルを Mod 発見時に非同期で1回だけ読み、通常は解放しない。シミュ開始が読込完了より早いこともある(起動直後の MP 参加など)。
	/// 未読込(<c>!bundle.Loaded</c>)なら <c>bundle.OnLoad</c> を購読して完了後に取り直し、シミュ停止で購読を解除する。
	/// 読込済みのバンドルの <c>OnLoad</c> を購読すると本体が全アセットを読み直すので、購読は未読込のときだけにする。
	/// </para>
	/// </summary>
	public static class BundleAssets
	{
		private static readonly Dictionary<ModAssetBundle, Dictionary<string, Object>> Cache =
			new Dictionary<ModAssetBundle, Dictionary<string, Object>>();

		/// <summary>未読込・読込エラー・見つからないときは null。メインスレッドから呼ぶ。シミュ開始時などに取り出して持っておき、毎フレームは呼ばない。</summary>
		public static T Load<T>(ModAssetBundle bundle, string name) where T : Object
		{
			if (bundle == null || string.IsNullOrEmpty(name) || !bundle.Loaded || bundle.HasError) return null;
			Dictionary<string, Object> assets;
			if (!Cache.TryGetValue(bundle, out assets)) Cache[bundle] = assets = new Dictionary<string, Object>();

			string key = typeof(T).FullName + "/" + name;
			Object cached;
			// 見つからなかった名前は本物の null で覚えている。破棄されたアセット(Unity の偽 null)は取り直す。
			if (assets.TryGetValue(key, out cached) && (ReferenceEquals(cached, null) || cached != null)) return (T)cached;

			var asset = bundle.LoadAsset<T>(name);
			assets[key] = asset != null ? asset : null;
			return asset;
		}
	}
}
