using UnityEngine;

namespace EWL
{
    // EWL が常駐させるオブジェクト(プールの置き場・同期ホスト等)の親。シーンに置くのはこの1つだけにする。
    // 飛行中の弾や消えかけのエフェクトはワールドの物なので、ここには入れずシーン直下に置く。
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
