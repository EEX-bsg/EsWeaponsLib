using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Modding.Modules;
using Modding.Serialization;
using UnityEngine;
using Vector3 = Modding.Serialization.Vector3;

namespace EWL.Modules
{
    // XML スキーマ(C# API ではない)。全要素省略可。省略した項目は本体の値のまま変えない。
    // DefaultValue は「省略可」の目印(本体は値を読まず、省略時は初期化子の値)。float・int・bool・enum は初期化子と同じ値を書く(enum に null は例外になる)。
    // Reloadable: Debug の Mod で XML を保存すると、その要素を丸ごと差し替える。シミュ用ブロックの生成時に適用されるので次のシミュから反映。
    // AdditionalColliders はビルドブロックに足して複製するものなので付けない。
    /// <summary>ブロック自体の物理(接合強度・物理マテリアル・接合の回転軸・追加コライダー)を設定するXMLカスタムモジュール。全要素を省略でき、省略した項目はBesiege本体の値のまま変えない。</summary>
    [XmlRoot("EwlBlockPhysics")]
    public class EwlBlockPhysicsModule : BlockModule
    {
        /// <summary>親ブロックとの接合の破断力・破断トルク。省略すると変えない。0は力が少しでもかかれば壊れる接合。壊れない接合はINF。</summary>
        [XmlElement, DefaultValue(-1f), Reloadable] public float JointStrength = -1f;

        /// <summary>書けばブロックのコライダー全部に物理マテリアルを設定する。</summary>
        [XmlElement, DefaultValue(null), Reloadable] public BlockMaterialXml Material;

        /// <summary>書けば接合の回転軸を設定する。</summary>
        [XmlElement, DefaultValue(null), Reloadable] public JointRotationXml Rotation;

        /// <summary>
        /// ブロックに足すコライダー。ブロックのCollidersと同じ書式(layer・trigger属性も同じ)で、BoxColliderはCenterも書ける。
        /// layer="12" にすると、他のブロックをそこに置け、シミュ開始時に置いたブロックがこのブロックへ接続する面になる。
        /// BoxColliderは6面のどこでも置け、クリックした点を面の中心から1マス単位に丸めた位置に置かれる。
        /// Sphere/Capsuleはバニラと同じく、コライダーのPosition + 当たった面の法線 × 0.5に置かれる(当たった点ではない)。
        /// Materialを書けば、これらにも適用する。
        /// </summary>
        [XmlArray("AdditionalColliders"), DefaultValue(null), CanBeEmpty]
        [XmlArrayItem("SphereCollider", typeof(SphereModCollider))]
        [XmlArrayItem("CapsuleCollider", typeof(CapsuleModCollider))]
        [XmlArrayItem("BoxCollider", typeof(BoxColliderXml))]
        public List<ModCollider> AdditionalColliders = new List<ModCollider>();
    }

    /// <summary>ブロックのBoxColliderにCenter(UnityのBoxCollider.center。Scale前の大きさ1の箱での座標)を足したもの。</summary>
    [Serializable]
    public class BoxColliderXml : BoxModCollider
    {
        [XmlElement, DefaultValue(null)] public Vector3 Center = UnityEngine.Vector3.zero;
    }

    [Serializable]
    public class BlockMaterialXml : Element
    {
        /// <summary>摩擦係数(静止・動摩擦とも)。</summary>
        [XmlElement, DefaultValue(0.6f)] public float Friction = 0.6f;
        /// <summary>反発係数(0〜1)。</summary>
        [XmlElement, DefaultValue(0f)] public float Bounciness = 0f;
        /// <summary>相手の摩擦との合成方法。</summary>
        [XmlElement, DefaultValue(PhysicMaterialCombine.Average)] public PhysicMaterialCombine FrictionCombine = PhysicMaterialCombine.Average;
        /// <summary>相手の反発との合成方法。</summary>
        [XmlElement, DefaultValue(PhysicMaterialCombine.Average)] public PhysicMaterialCombine BounceCombine = PhysicMaterialCombine.Average;
    }

    /// <summary>軸はLocked / Limited / Free。Spring・Damperは回転を元の向きへ戻す力、LimitはLimitedの可動角[度]。</summary>
    [Serializable]
    public class JointRotationXml : Element
    {
        /// <summary>X軸まわりの回転(Locked = 固定 / Limited = Limitまで / Free = 自由)。</summary>
        [XmlElement, DefaultValue(ConfigurableJointMotion.Locked)] public ConfigurableJointMotion AxisX = ConfigurableJointMotion.Locked;
        /// <summary>Y軸まわりの回転。</summary>
        [XmlElement, DefaultValue(ConfigurableJointMotion.Locked)] public ConfigurableJointMotion AxisY = ConfigurableJointMotion.Locked;
        /// <summary>Z軸まわりの回転。</summary>
        [XmlElement, DefaultValue(ConfigurableJointMotion.Locked)] public ConfigurableJointMotion AxisZ = ConfigurableJointMotion.Locked;
        /// <summary>元の向きへ戻すばねの強さ。0なら戻さない。</summary>
        [XmlElement, DefaultValue(0f)] public float Spring = 0f;
        /// <summary>ばねの減衰。</summary>
        [XmlElement, DefaultValue(0f)] public float Damper = 0f;
        /// <summary>Limitedの軸の可動角 [度]。</summary>
        [XmlElement, DefaultValue(0f)] public float Limit = 0f;
    }
}
