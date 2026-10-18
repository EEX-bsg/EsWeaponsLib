using EWL.Besiege;
using UnityEngine;

namespace EWL
{
    // コンソールコマンド ewl_dummy: 誘導のテスト用のダミー標的を置く・消す(SP で敵がいなくても照準・誘導を確かめるため)。
    // ダミーはこのピアだけで全員の敵として扱う(Weapon.FindTarget・Target で選べる)。Besiege のプレイヤー・マシンではなく、MP で同期しない。
    // 当たり判定(箱)があり、弾が当たる。シーンと一緒に消える。
    internal static class TestTargets
    {
        private const float DefaultDistance = 150f;
        private const float Size = 4f; // 戦車ほどの大きさ [m]
        private static Transform _parent;
        private static Material _material; // 全ダミーで共有(作るたびにマテリアルを複製しない)

        internal static void Command(string[] args)
        {
            MachineTargets.Dummies.RemoveAll(dummy => dummy == null);
            string action = args.Length > 0 ? args[0] : "";
            if (action == "add")
            {
                float distance;
                if (args.Length < 2 || !float.TryParse(args[1], out distance)) distance = DefaultDistance;
                Log(Add(distance));
            }
            else if (action == "clear")
            {
                foreach (var dummy in MachineTargets.Dummies) Object.Destroy(dummy.gameObject);
                MachineTargets.Dummies.Clear();
                Log("[EWL] dummy targets cleared");
            }
            else
            {
                Log("[EWL] dummy targets: " + MachineTargets.Dummies.Count + ". Usage: ewl_dummy add [distance] | clear");
            }
        }

        // 視点カメラの水平な前方 distance [m] の地面に置く
        private static string Add(float distance)
        {
            var orbit = MouseOrbit.Instance;
            var camera = orbit != null ? orbit.cam : null;
            if (camera == null) return "[EWL] dummy target: no camera";
            var forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            var position = camera.transform.position + forward.normalized * distance;
            RaycastHit ground;
            if (Physics.Raycast(position + Vector3.up * 500f, Vector3.down, out ground, 1000f)) position = ground.point;
            position += Vector3.up * (Size * 0.5f);

            if (_parent == null) _parent = SceneRoot.CreateChild("DummyTargets");
            var dummy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dummy.name = "EwlDummyTarget";
            dummy.transform.SetParent(_parent, false);
            dummy.transform.position = position;
            dummy.transform.localScale = Vector3.one * Size;
            var renderer = dummy.GetComponent<Renderer>();
            if (_material == null) _material = new Material(renderer.sharedMaterial) { color = new Color(1f, 0.3f, 0.2f) };
            renderer.sharedMaterial = _material;
            MachineTargets.Dummies.Add(dummy.transform);
            return "[EWL] dummy target added at " + position;
        }

        private static void Log(string message)
        {
            Modding.ModConsole.Log(message);
        }
    }
}
