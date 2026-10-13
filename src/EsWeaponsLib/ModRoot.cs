using UnityEngine;

namespace EWL
{
    // EWL が常駐させるオブジェクト(同期ホスト・音の見本など、ゲーム終了まで動く仕組み)の親。DontDestroyOnLoad。
    // シーンと一緒に捨てるもの(弾・エフェクトのプール)は SceneRoot に置く。
    internal static class ModRoot
    {
        private static Transform _root;

        internal static Transform CreateChild(string name)
        {
            if (_root == null)
            {
                _root = new GameObject("EsWeaponsLib").transform;
                Object.DontDestroyOnLoad(_root.gameObject);
            }
            var child = new GameObject(name).transform;
            child.SetParent(_root, false);
            return child;
        }
    }
}
