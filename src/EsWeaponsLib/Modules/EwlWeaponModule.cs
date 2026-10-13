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
    // DefaultValue は「省略可」の目印(本体は値を読まず、省略時は初期化子の値)。float・int・bool・enum は初期化子と同じ値を書く(enum に null は例外になる)。
    // Reloadable: Debug の Mod で XML を保存すると、その要素を丸ごと差し替える(子要素の中の個別指定は効かない)。次のシミュ開始から反映。
    // キー・トグル・スライダーはブロック生成時にしか読まないので付けない。
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

        /// <summary>値 [発/s] の逆数を Fire/Interval に使う(XML の値より優先。0 以下なら撃たない)。発射ごとに読む。</summary>
        [XmlElement, DefaultValue(null)]
        public MSliderReference FireRateSlider;

        /// <summary>値 [s] を Fire/Delay に加える。発射ごとに読む。</summary>
        [XmlElement, DefaultValue(null)]
        public MSliderReference FireDelaySlider;

        /// <summary>値 [s] を Projectile/TimeFuse に使う(XML の値より優先)。Projectile/TimeFuseRandomExtra の乱数は別に加わる。発射ごとに読み、飛行中の弾は変えない。</summary>
        [XmlElement, DefaultValue(null)]
        public MSliderReference TimeFuseSlider;

        /// <summary>値を四捨五入して Fire/MagazineCapacity に使う(XML の値より優先)。シミュ開始時に読む。</summary>
        [XmlElement, DefaultValue(null)]
        public MSliderReference MagazineCapacitySlider;

        [XmlElement, DefaultValue(null), Reloadable] public MuzzleXml Muzzle = new MuzzleXml();
        [XmlElement, DefaultValue(null), Reloadable] public FireXml Fire = new FireXml();
        [XmlElement, DefaultValue(null), Reloadable] public ProjectileXml Projectile = new ProjectileXml();
        [XmlElement, DefaultValue(null), Reloadable] public ImpactXml Impact = new ImpactXml();
        [XmlElement, DefaultValue(null), Reloadable] public EffectsXml Effects = new EffectsXml();
        /// <summary>弾の推進(ロケット)。Thrust [N] が 0 なら推進しない。</summary>
        [XmlElement, DefaultValue(null), Reloadable] public BoosterXml Booster = new BoosterXml();
        /// <summary>誘導(仮実装。要素ごと今後破壊的に変更する予定)。Gain が 0 なら誘導しない。</summary>
        [XmlElement, DefaultValue(null), Reloadable] public GuidanceXml Guidance = new GuidanceXml();
    }

    [Serializable]
    public class GuidanceXml : Element
    {
        [XmlElement, DefaultValue(0f)] public float Gain = 0f;
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
        [XmlElement, DefaultValue(100f)] public float Velocity = 100f;
        [XmlElement, DefaultValue(0.5f)] public float Interval = 0.5f;
        [XmlElement, DefaultValue(true)] public bool Hold = true;
        [XmlElement, DefaultValue(0f)] public float Spread = 0f;
        [XmlElement, DefaultValue(0f)] public float Delay = 0f;
        [XmlElement, DefaultValue(0f)] public float DelayRandomExtra = 0f;
        [XmlElement, DefaultValue(1f)] public float Recoil = 1f;
        [XmlElement, DefaultValue(-1)] public int Ammo = -1;
        [XmlElement, DefaultValue(1)] public int BurstCount = 1;
        [XmlElement, DefaultValue(0.1f)] public float BurstInterval = 0.1f;
        [XmlElement, DefaultValue(0)] public int MagazineCapacity = 0;
        [XmlElement, DefaultValue(1f)] public float ReloadTime = 1f;
        [XmlElement, DefaultValue(true)] public bool AutoReload = true;
        /// <summary>発射時に弾のローカル軸で足す速度 [m/s]。</summary>
        [XmlElement, DefaultValue(null)] public Vector3 EjectVelocity = UnityEngine.Vector3.zero;
    }

    [Serializable]
    public class BoosterXml : Element
    {
        [XmlElement, DefaultValue(0f)] public float Thrust = 0f;
        [XmlElement, DefaultValue(0f)] public float BurnTime = 0f;
        [XmlElement, DefaultValue(0f)] public float LateralDrag = 0f;
    }

    [Serializable]
    public class ProjectileXml : Element
    {
        [XmlElement, DefaultValue(1f)] public float Mass = 1f;
        [XmlElement, DefaultValue(0f)] public float Drag = 0f;
        [XmlElement, DefaultValue(0f)] public float AngularDrag = 0f;
        [XmlElement, DefaultValue(false)] public bool IgnoreGravity = false;
        [XmlElement, DefaultValue(CollisionDetectionMode.ContinuousDynamic)] public CollisionDetectionMode CollisionMode = CollisionDetectionMode.ContinuousDynamic;
        [XmlElement, DefaultValue(20f)] public float Lifetime = 20f;
        [XmlElement, DefaultValue(0f)] public float TimeFuse = 0f;
        [XmlElement, DefaultValue(0.05f)] public float TimeFuseRandomExtra = 0.05f;
        [XmlElement, DefaultValue(0.02f)] public float ColliderEnableDelay = 0.02f;
        [XmlElement, DefaultValue(50)] public int PoolSize = 50;
        [XmlElement, DefaultValue(0.6f)] public float Friction = 0.6f;
        [XmlElement, DefaultValue(0f)] public float Bounciness = 0f;
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
        /// <summary>撃てる状態の間、次の弾の見た目を発射位置に表示する。</summary>
        [XmlElement, DefaultValue(false)] public bool ShowWhenLoaded = false;
    }

    [Serializable]
    public class ImpactXml : Element
    {
        [XmlElement, DefaultValue(0f)] public float BlockDamage = 0f;
        [XmlElement, DefaultValue(0f)] public float EntityDamage = 0f;
        [XmlElement, DefaultValue(10f)] public float ImpactForce = 10f;
        [XmlElement, DefaultValue(false)] public bool Attach = false;
        [XmlElement, DefaultValue(0f)] public float ExplosionRadius = 0f;
        [XmlElement, DefaultValue(false)] public bool ExplodesOnlyWithGodTool = false;
        [XmlElement, DefaultValue(0f)] public float ExplosionPower = 0f;
        [XmlElement, DefaultValue(0f)] public float ExplosionUpPower = 0f;
        [XmlElement, DefaultValue(0f)] public float DetonationDelay = 0f;
        [XmlElement, DefaultValue(0f)] public float DetonationDelayRandomExtra = 0f;
        [XmlElement, DefaultValue(false)] public bool DespawnOnHit = false;
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
        [XmlElement, DefaultValue(false)] public bool ExplosionFacesNormal = false;

        [XmlArray("FireSounds"), XmlArrayItem("AudioClip"), DefaultValue(null), CanBeEmpty]
        public List<ResourceReference> FireSounds = new List<ResourceReference>();

        [XmlArray("ExplosionSounds"), XmlArrayItem("AudioClip"), DefaultValue(null), CanBeEmpty]
        public List<ResourceReference> ExplosionSounds = new List<ResourceReference>();

        [XmlArray("ReloadSounds"), XmlArrayItem("AudioClip"), DefaultValue(null), CanBeEmpty]
        public List<ResourceReference> ReloadSounds = new List<ResourceReference>();
    }
}
