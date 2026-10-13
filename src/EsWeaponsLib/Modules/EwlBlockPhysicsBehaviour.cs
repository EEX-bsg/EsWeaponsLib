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
        private const string AttachFaceName = "EwlAttachFace";
        private const int AttachLayer = 12;
        private const float AttachFaceGap = 0.01f;

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
                var box = collider as BoxCollider;
                var boxXml = shape as BoxColliderXml;
                if (boxXml != null) box.center = boxXml.Center;
                if (box != null && collider.gameObject.layer == AttachLayer) AddAttachFaces(box);
            }
        }

        // 本体はクリックしたコライダーの原点 + 法線 × 0.5 にブロックを置く(箱の中心・大きさは見ない)ので、大きな箱の面には置けない。
        // 面ごとに本体の AddPointMultiSnap の板を重ねる: 原点を面の 0.5 内側に置けば、当たった点を1マス単位に丸めた面上の位置になる。
        // 板の中心(center)の向きが本体の丸めの法線になるので、板の面は板の +Z に置く。
        private void AddAttachFaces(BoxCollider box)
        {
            var boxTransform = box.transform;
            var size = Vector3.Scale(boxTransform.localScale, box.size);
            var middle = boxTransform.localPosition + boxTransform.localRotation * Vector3.Scale(boxTransform.localScale, box.center);
            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    var normal = Vector3.zero;
                    normal[axis] = sign;
                    var faceRotation = Quaternion.LookRotation(normal, axis == 1 ? Vector3.forward : Vector3.up);
                    var faceSize = Quaternion.Inverse(faceRotation) * size;

                    var face = new GameObject(AttachFaceName);
                    face.layer = AttachLayer;
                    face.tag = "AddPointMultiSnap";
                    face.transform.parent = transform;
                    face.transform.localPosition = middle + boxTransform.localRotation * normal * (size[axis] / 2f - 0.5f);
                    face.transform.localRotation = boxTransform.localRotation * faceRotation;
                    face.transform.localScale = Vector3.one;
                    var plate = face.AddComponent<BoxCollider>();
                    plate.isTrigger = true;
                    // 箱の面よりわずかに外へ出し、クリックが箱より先に板へ当たるようにする
                    plate.center = new Vector3(0f, 0f, 0.5f + AttachFaceGap);
                    plate.size = new Vector3(Mathf.Abs(faceSize.x), Mathf.Abs(faceSize.y), 0f);
                }
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
