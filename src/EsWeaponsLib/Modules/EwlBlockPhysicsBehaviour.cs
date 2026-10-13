using Modding.Modules;
using UnityEngine;

namespace EWL.Modules
{
    // EwlBlockPhysics モジュール。ブロック自身の接合・物理マテリアルを XML の値にし、コライダーを足す。
    // SafeAwake で書く: 本体が接合の破断力を記録する(ダメージ計算の基準)のはこれより後なので、ここで書いた値が基準になる。
    // ponytail: 衝突検出モード・並進軸・アンカー移動・スライダー連動は未対応。E砲で必要になったら足す。
    public class EwlBlockPhysicsBehaviour : BlockModuleBehaviour<EwlBlockPhysicsModule>
    {
        private const string AdditionalColliderName = "EwlAdditionalCollider";

        private PhysicMaterial _material;

        public override void SafeAwake()
        {
            // 親ブロックとの接合。他モジュールが足した分も含めて全部に適用する
            foreach (var joint in GetComponents<ConfigurableJoint>())
            {
                if (Module.JointStrength > 0f)
                {
                    joint.breakForce = Module.JointStrength;
                    joint.breakTorque = Module.JointStrength;
                }
                if (Module.Rotation != null) ApplyRotation(joint, Module.Rotation);
            }
            // シミュ用ブロックはビルドブロックの複製なので、足したコライダーも複製済み。二重に足さない
            if (Module.AdditionalColliders != null && transform.Find(AdditionalColliderName) == null) AddColliders();
            if (Module.Material != null) ApplyMaterial(Module.Material);
        }

        // ブロックのルート直下に置く: 本体の接続トリガーは、親がブロック自身のコライダーを接続先にしない(自分への接続を防ぐ)
        private void AddColliders()
        {
            foreach (var shape in Module.AdditionalColliders)
            {
                var collider = shape.CreateCollider(transform);
                collider.gameObject.name = AdditionalColliderName;
                var box = shape as BoxColliderXml;
                if (box != null) ((BoxCollider)collider).center = box.Center;
            }
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        private static void ApplyRotation(ConfigurableJoint joint, JointRotationXml rotation)
        {
            joint.angularXMotion = rotation.AxisX;
            joint.angularYMotion = rotation.AxisY;
            joint.angularZMotion = rotation.AxisZ;

            var limit = new SoftJointLimit { limit = rotation.Limit };
            joint.lowAngularXLimit = new SoftJointLimit { limit = -rotation.Limit };
            joint.highAngularXLimit = limit;
            joint.angularYLimit = limit;
            joint.angularZLimit = limit;

            var twistDrive = joint.angularXDrive;
            twistDrive.positionSpring = rotation.Spring;
            twistDrive.positionDamper = rotation.Damper;
            joint.angularXDrive = twistDrive;
            var swingDrive = joint.angularYZDrive;
            swingDrive.positionSpring = rotation.Spring;
            swingDrive.positionDamper = rotation.Damper;
            joint.angularYZDrive = swingDrive;
        }

        private void ApplyMaterial(BlockMaterialXml material)
        {
            _material = new PhysicMaterial("EwlBlock")
            {
                staticFriction = material.Friction,
                dynamicFriction = material.Friction,
                frictionCombine = material.FrictionCombine,
                bounciness = material.Bounciness,
                bounceCombine = material.BounceCombine,
            };
            var colliders = transform.Find("Colliders");
            if (colliders != null)
            {
                foreach (var collider in colliders.GetComponentsInChildren<Collider>()) collider.sharedMaterial = _material;
            }
            else
            {
                Debug.LogWarning("[EWL] " + name + " EwlBlockPhysics/Material: block has no 'Colliders' child.");
            }
            foreach (Transform child in transform)
            {
                if (child.name == AdditionalColliderName) child.GetComponent<Collider>().sharedMaterial = _material;
            }
        }
    }
}
