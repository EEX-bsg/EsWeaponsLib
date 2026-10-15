using EWL.Api;
using UnityEngine;

namespace EWL.Weapons
{
    // 発射位置に置く装填中の弾の見た目(コライダーなし)。シミュ中の武装と、ビルド中のプレビューで共用する。
    internal sealed class LoadedProjectileVisual : MonoBehaviour
    {
        // ビルド中のプレビューか。シミュ開始で本体がマシンを丸ごと複製するので、複製側(シミュ中のブロック)では自分を消す。
        // 複製に引き継ぐためシリアライズする。
        [SerializeField] private bool _buildPreview;
        private MeshFilter _meshFilter;
        private MeshRenderer _renderer;

        internal static LoadedProjectileVisual Create(Transform parent, bool buildPreview)
        {
            var root = new GameObject("EwlLoadedProjectile");
            root.transform.SetParent(parent, false);
            var visual = new GameObject("Vis");
            visual.transform.SetParent(root.transform, false);
            var loaded = root.AddComponent<LoadedProjectileVisual>();
            loaded._buildPreview = buildPreview;
            loaded._meshFilter = visual.AddComponent<MeshFilter>();
            loaded._renderer = visual.AddComponent<MeshRenderer>();
            return loaded;
        }

        // Awake ではブロックのシミュ中フラグが確定していないことがあるので Start で見る
        private void Start()
        {
            if (!_buildPreview) return;
            var block = GetComponentInParent<BlockBehaviour>();
            if (block != null && block.isSimulating) Destroy(gameObject);
        }

        // Spec は書き換えられうるので、武装は表示中に毎フレーム呼ぶ(割り当てなし)。
        internal void Apply(WeaponSpec spec)
        {
            var projectile = spec.Projectile;
            var root = transform;
            root.localPosition = spec.MuzzlePosition;
            root.localRotation = Quaternion.Euler(spec.MuzzleRotation);
            var visualTransform = _meshFilter.transform;
            if (projectile.Mesh != null)
            {
                visualTransform.localPosition = projectile.MeshPosition;
                visualTransform.localRotation = Quaternion.Euler(projectile.MeshRotation);
                visualTransform.localScale = projectile.MeshScale;
                _meshFilter.sharedMesh = projectile.Mesh;
            }
            else
            {
                visualTransform.localPosition = Vector3.zero;
                visualTransform.localRotation = Quaternion.identity;
                visualTransform.localScale = Vector3.one * (Projectile.DefaultRadius * 2f);
                _meshFilter.sharedMesh = Projectile.SphereMesh();
            }
            _renderer.sharedMaterial = ProjectileMaterials.For(projectile.Texture);
        }
    }
}
