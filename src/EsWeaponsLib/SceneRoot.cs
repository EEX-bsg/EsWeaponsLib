using UnityEngine;

namespace EWL
{
    // シーンに属する EWL のオブジェクト(弾・エフェクトのプールなど)の親。シーン直下の EWL のオブジェクトはこれだけにする。
    // シーンと一緒に破棄され、次に要るときに作り直す(シーンをまたいで持ち越さない)。呼び出し側は子が null になったら作り直す。
    // 例外は刺さった弾と弾に付いたエフェクトで、追従のため相手の子になる。
    // 原点・無回転・等倍から動かさない(子の弾・エフェクトはワールド座標をそのままローカル座標として扱う)。
    internal static class SceneRoot
    {
        private static Transform _root;

        internal static Transform CreateChild(string name)
        {
            if (_root == null) _root = new GameObject("EsWeaponsLib (Scene)").transform;
            var child = new GameObject(name).transform;
            child.SetParent(_root, false);
            return child;
        }
    }
}
