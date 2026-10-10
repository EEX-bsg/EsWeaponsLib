using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Modding.Modules;
using Modding.Serialization;
using UnityEngine;

namespace EWL.Modules
{
	// XML スキーマ(C# API ではない)。全要素省略可。省略した項目は本体の値のまま変えない。
	// 注意: enum 型の要素に DefaultValue(null) を付けると XmlSerializer が例外になる。enum は実際の値を書く。
	[XmlRoot("EwlBlockPhysics")]
	public class EwlBlockPhysicsModule : BlockModule
	{
		/// <summary>親ブロックとの接合の破断力・破断トルク。0 = 変えない。壊れない接合は INF。</summary>
		[XmlElement, DefaultValue(null)] public float JointStrength = 0f;

		/// <summary>書けばブロックのコライダー全部に物理マテリアルを設定する。</summary>
		[XmlElement, DefaultValue(null)] public BlockMaterialXml Material;

		/// <summary>書けば接合の回転軸を設定する。</summary>
		[XmlElement, DefaultValue(null)] public JointRotationXml Rotation;
	}

	[Serializable]
	public class BlockMaterialXml : Element
	{
		[XmlElement, DefaultValue(null)] public float Friction = 0.6f;
		[XmlElement, DefaultValue(null)] public float Bounciness = 0f;
		[XmlElement, DefaultValue(PhysicMaterialCombine.Average)] public PhysicMaterialCombine FrictionCombine = PhysicMaterialCombine.Average;
		[XmlElement, DefaultValue(PhysicMaterialCombine.Average)] public PhysicMaterialCombine BounceCombine = PhysicMaterialCombine.Average;
	}

	/// <summary>軸は Locked / Limited / Free。Spring・Damper は回転を元の向きへ戻す力、Limit は Limited の可動角[度]。</summary>
	[Serializable]
	public class JointRotationXml : Element
	{
		[XmlElement, DefaultValue(ConfigurableJointMotion.Locked)] public ConfigurableJointMotion AxisX = ConfigurableJointMotion.Locked;
		[XmlElement, DefaultValue(ConfigurableJointMotion.Locked)] public ConfigurableJointMotion AxisY = ConfigurableJointMotion.Locked;
		[XmlElement, DefaultValue(ConfigurableJointMotion.Locked)] public ConfigurableJointMotion AxisZ = ConfigurableJointMotion.Locked;
		[XmlElement, DefaultValue(null)] public float Spring = 0f;
		[XmlElement, DefaultValue(null)] public float Damper = 0f;
		[XmlElement, DefaultValue(null)] public float Limit = 0f;
	}
}
