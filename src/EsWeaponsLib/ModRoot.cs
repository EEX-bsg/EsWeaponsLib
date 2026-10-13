using UnityEngine;

namespace EWL
{
    // EWL が作るオブジェクト(弾・エフェクトのプールの置き場・同期ホスト等)の親。シーン直下に置くのはこの1つだけにする。
    // 例外は刺さった弾と弾に付いたエフェクトで、追従のため相手の子になる。
    // 原点・無回転・等倍から動かさない(子の弾・エフェクトはワールド座標をそのままローカル座標として扱う)。
    internal static class ModRoot
    {
        private static Transform _root;

        internal static Transform CreateChild(string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(Root(), false);
            return child;
        }

        private static Transform Root()
        {
            if (_root != null) return _root;
            var root = new GameObject("EsWeaponsLib");
            Object.DontDestroyOnLoad(root);
            _root = root.transform;
            return _root;
        }
    }
}
