using System.Collections.Generic;
using UnityEngine;

namespace EWL.Weapons
{
    // 弾のマテリアル・物理マテリアルを、同じテクスチャ・同じ値どうしで共有する。武装が消えても弾は飛び続けるので
    // (MP で撃った人だけがビルドに戻った場合など)、武装には持たせず破棄もしない。
    // 数は使われたテクスチャ・値の組の種類だけ(Spec の値を発射ごとに連続的に変える使い方では増え続ける。割り切り)。
    internal static class ProjectileMaterials
    {
        private sealed class PhysicEntry
        {
            internal float Friction;
            internal PhysicMaterialCombine FrictionCombine;
            internal float Bounciness;
            internal PhysicMaterialCombine BounceCombine;
            internal PhysicMaterial Material;
        }

        private static readonly Dictionary<Texture, Material> Materials = new Dictionary<Texture, Material>();
        private static readonly List<PhysicEntry> PhysicMaterials = new List<PhysicEntry>();
        private static Material _untexturedMaterial;
        private static bool _shaderMissing;

        internal static Material For(Texture texture)
        {
            Material material;
            if (texture == null) material = _untexturedMaterial;
            else Materials.TryGetValue(texture, out material);
            if (material != null || _shaderMissing) return material;
            // Standard は Besiege のビルドで紫になることがあるので Legacy Diffuse を優先
            var shader = Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Diffuse") ?? Shader.Find("Standard");
            if (shader == null)
            {
                _shaderMissing = true;
                Debug.LogWarning("[EWL] No usable shader for projectile material.");
                return null;
            }
            material = new Material(shader) { mainTexture = texture };
            if (texture == null) _untexturedMaterial = material;
            else Materials[texture] = material;
            return material;
        }

        // 比較は要求値で行う(Unity が丸めた読み戻し値と比べると一致せず、発射ごとに増えるため)。
        internal static PhysicMaterial For(float friction, PhysicMaterialCombine frictionCombine, float bounciness, PhysicMaterialCombine bounceCombine)
        {
            foreach (var entry in PhysicMaterials)
                if (entry.Material != null && entry.Friction == friction && entry.FrictionCombine == frictionCombine
                    && entry.Bounciness == bounciness && entry.BounceCombine == bounceCombine) return entry.Material;
            var physicMaterial = new PhysicMaterial("EwlProjectile")
            {
                staticFriction = friction,
                dynamicFriction = friction,
                frictionCombine = frictionCombine,
                bounciness = bounciness,
                bounceCombine = bounceCombine,
            };
            PhysicMaterials.Add(new PhysicEntry
            {
                Friction = friction,
                FrictionCombine = frictionCombine,
                Bounciness = bounciness,
                BounceCombine = bounceCombine,
                Material = physicMaterial,
            });
            return physicMaterial;
        }
    }
}
