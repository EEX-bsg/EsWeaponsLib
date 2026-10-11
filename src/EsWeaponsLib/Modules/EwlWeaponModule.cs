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
	// XML スキーマ(C# API ではない)。FireKey 以外は全て省略可・既定値は EWL.Api の各 Spec と同じ。
	// 値の意味は EWL.Api.WeaponSpec のコメントを参照。
	// 注意: enum 型の要素に DefaultValue(null) を付けると XmlSerializer が例外になる。enum は実際の値を書く。
	[XmlRoot("EwlWeapon")]
	public class EwlWeaponModule : BlockModule
	{
		[XmlElement, RequireToValidate]
		public MKeyReference FireKey;

		/// <summary>ON: 押している間連射 / OFF: 押すたびに連射の開始・停止を切り替え。省略時は Fire/Hold。</summary>
		[XmlElement, DefaultValue(null)]
		public MToggleReference HoldToFireToggle;

		/// <summary>押すとリロード(Fire/MagazineCapacity が 1 以上のとき)。</summary>
		[XmlElement, DefaultValue(null)]
		public MKeyReference ReloadKey;

		/// <summary>ON で弾倉が空になったら自動リロード。省略時は Fire/AutoReload。</summary>
		[XmlElement, DefaultValue(null)]
		public MToggleReference AutoReloadToggle;

		/// <summary>値 [s] を Fire/Delay に加える。</summary>
		[XmlElement, DefaultValue(null)]
		public MSliderReference DelaySlider;

		/// <summary>値 [s] を Projectile/TimeFuse に使う(XML の値より優先)。</summary>
		[XmlElement, DefaultValue(null)]
		public MSliderReference TimeFuseSlider;

		[XmlElement, DefaultValue(null)] public MuzzleXml Muzzle = new MuzzleXml();
		[XmlElement, DefaultValue(null)] public FireXml Fire = new FireXml();
		[XmlElement, DefaultValue(null)] public ProjectileXml Projectile = new ProjectileXml();
		[XmlElement, DefaultValue(null)] public ImpactXml Impact = new ImpactXml();
		[XmlElement, DefaultValue(null)] public EffectsXml Effects = new EffectsXml();
	}

	[Serializable]
	public class MuzzleXml : Element
	{
		[XmlElement, DefaultValue(null)] public Vector3 Position = UnityEngine.Vector3.zero;
		[XmlElement, DefaultValue(null)] public Vector3 Rotation = UnityEngine.Vector3.zero;
	}

	[Serializable]
	public class FireXml : Element
	{
		[XmlElement, DefaultValue(null)] public float Velocity = 100f;
		[XmlElement, DefaultValue(null)] public float Interval = 0.5f;
		[XmlElement, DefaultValue(null)] public bool Hold = true;
		[XmlElement, DefaultValue(null)] public float Spread = 0f;
		[XmlElement, DefaultValue(null)] public float Delay = 0f;
		[XmlElement, DefaultValue(null)] public float RandomDelay = 0f;
		[XmlElement, DefaultValue(null)] public float Recoil = 1f;
		[XmlElement, DefaultValue(null)] public int Ammo = -1;
		[XmlElement, DefaultValue(null)] public int BurstCount = 1;
		[XmlElement, DefaultValue(null)] public float BurstInterval = 0.1f;
		[XmlElement, DefaultValue(null)] public int MagazineCapacity = 0;
		[XmlElement, DefaultValue(null)] public float ReloadTime = 1f;
		[XmlElement, DefaultValue(null)] public bool AutoReload = true;
	}

	[Serializable]
	public class ProjectileXml : Element
	{
		[XmlElement, DefaultValue(null)] public float Mass = 1f;
		[XmlElement, DefaultValue(null)] public float Drag = 0f;
		[XmlElement, DefaultValue(null)] public float AngularDrag = 0f;
		[XmlElement, DefaultValue(null)] public bool IgnoreGravity = false;
		[XmlElement, DefaultValue(CollisionDetectionMode.ContinuousDynamic)] public CollisionDetectionMode CollisionMode = CollisionDetectionMode.ContinuousDynamic;
		[XmlElement, DefaultValue(null)] public float Lifetime = 20f;
		[XmlElement, DefaultValue(null)] public float TimeFuse = 0f;
		[XmlElement, DefaultValue(null)] public int PoolSize = 50;
		[XmlElement, DefaultValue(null)] public float Friction = 0.6f;
		[XmlElement, DefaultValue(null)] public float Bounciness = 0f;
		[XmlElement, DefaultValue(PhysicMaterialCombine.Average)] public PhysicMaterialCombine FrictionCombine = PhysicMaterialCombine.Average;
		[XmlElement, DefaultValue(PhysicMaterialCombine.Average)] public PhysicMaterialCombine BounceCombine = PhysicMaterialCombine.Average;

		/// <summary>ブロックの Colliders と同じ書式(SphereCollider / CapsuleCollider / BoxCollider)。</summary>
		[XmlArray("Colliders"), DefaultValue(null), CanBeEmpty]
		[XmlArrayItem("SphereCollider", typeof(SphereModCollider))]
		[XmlArrayItem("CapsuleCollider", typeof(CapsuleModCollider))]
		[XmlArrayItem("BoxCollider", typeof(BoxModCollider))]
		public List<ModCollider> Colliders = new List<ModCollider>();

		/// <summary>ブロックを定義した Mod のリソース名。</summary>
		[XmlElement, DefaultValue(null)] public MeshReference Mesh;
		[XmlElement, DefaultValue(null)] public ResourceReference Texture;
	}

	[Serializable]
	public class ImpactXml : Element
	{
		[XmlElement, DefaultValue(null)] public float BlockDamage = 0f;
		[XmlElement, DefaultValue(null)] public float EntityDamage = 0f;
		[XmlElement, DefaultValue(null)] public float ImpactForce = 10f;
		[XmlElement, DefaultValue(null)] public bool Attach = false;
		[XmlElement, DefaultValue(null)] public float ExplosionRadius = 0f;
		[XmlElement, DefaultValue(null)] public float ExplosionPower = 0f;
		[XmlElement, DefaultValue(null)] public float ExplosionUpPower = 0f;
		[XmlElement, DefaultValue(null)] public float FuseDelay = 0f;
		[XmlElement, DefaultValue(null)] public float FuseRandom = 0f;
		[XmlElement, DefaultValue(null)] public bool DespawnOnHit = false;
	}

	/// <summary>プレハブ名は AssetBundle 内のアセット名。Mac/Linux では「バンドル名+Mac」があればそちらを使う。</summary>
	[Serializable]
	public class EffectsXml : Element
	{
		[XmlElement, DefaultValue(null)] public ResourceReference AssetBundle;
		[XmlElement, DefaultValue(null)] public string ShotFlash;
		[XmlElement, DefaultValue(null)] public Vector3 ShotFlashPosition = UnityEngine.Vector3.zero;
		[XmlElement, DefaultValue(null)] public Vector3 ShotFlashRotation = UnityEngine.Vector3.zero;
		[XmlElement, DefaultValue(null)] public Vector3 ShotFlashScale = UnityEngine.Vector3.one;
		[XmlElement, DefaultValue(null)] public string Trail;
		[XmlElement, DefaultValue(null)] public string Bullet;
		[XmlElement, DefaultValue(null)] public string Explosion;
		[XmlElement, DefaultValue(null)] public bool ExplosionFacesNormal = false;

		[XmlArray("FireSounds"), XmlArrayItem("AudioClip"), DefaultValue(null), CanBeEmpty]
		public List<ResourceReference> FireSounds = new List<ResourceReference>();

		[XmlArray("ExplosionSounds"), XmlArrayItem("AudioClip"), DefaultValue(null), CanBeEmpty]
		public List<ResourceReference> ExplosionSounds = new List<ResourceReference>();

		[XmlArray("ReloadSounds"), XmlArrayItem("AudioClip"), DefaultValue(null), CanBeEmpty]
		public List<ResourceReference> ReloadSounds = new List<ResourceReference>();
	}
}
