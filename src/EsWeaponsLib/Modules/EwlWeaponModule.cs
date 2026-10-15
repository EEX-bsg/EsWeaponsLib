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
    // 要素名は EWL.Api の WeaponSpec / ProjectileSpec / ExplosionSpec / BoosterSpec / GuidanceSpec のフィールド名と同じで、値の意味はそのコメントを参照。
    // 違うのは、キー・トグル・スライダー・HoldToFire(モジュールだけの入力設定)と、プレハブ名・リソース名で書く項目。
    // DefaultValue は「省略可」の目印(本体は値を読まず、省略時は初期化子の値)。float・int・bool・enum は初期化子と同じ値を書く(enum に null は例外になる)。
    // Reloadable: Debug の Mod で XML を保存すると、その要素の値を差し替える(Element 型は要素ごと)。次のシミュ開始から反映。
    // キー・トグル・スライダーはブロック生成時にしか読まないので付けない。
    /// <summary>ブロックを武装にするXMLカスタムモジュール。1ブロックに複数書ける(それぞれが別の武装になる)。FireKey以外は省略でき、省略した要素は既定値になる。</summary>
    [XmlRoot("EwlWeapon")]
    public class EwlWeaponModule : BlockModule
    {
        /// <summary>発射キー(必須)。ブロックのModuleMapperTypesに定義したキーの名前をkey属性で書く。</summary>
        [XmlElement, RequireToValidate]
        public MKeyReference FireKey;

        /// <summary>ON: 押している間連射 / OFF: 押すたびに連射の開始・停止を切り替え。省略時はHoldToFire。</summary>
        [XmlElement, DefaultValue(null)]
        public MToggleReference HoldToFireToggle;

        /// <summary>押すとリロード(MagazineCapacityが1以上のとき)。</summary>
        [XmlElement, DefaultValue(null)]
        public MKeyReference ReloadKey;

        /// <summary>ONで弾倉が空になったら自動リロード。省略時はAutoReload。</summary>
        [XmlElement, DefaultValue(null)]
        public MToggleReference AutoReloadToggle;

        /// <summary>値 [発/s] の逆数をFireIntervalに使う(XMLの値より優先。0以下なら撃たない)。発射ごとに読む。</summary>
        [XmlElement, DefaultValue(null)]
        public MSliderReference FireRateSlider;

        /// <summary>値 [s] をFireDelayに加える。発射ごとに読む。</summary>
        [XmlElement, DefaultValue(null)]
        public MSliderReference FireDelaySlider;

        /// <summary>値 [s] をProjectile/TimeFuseに使う(XMLの値より優先)。Projectile/TimeFuseRandomExtraの乱数は別に加わる。発射ごとに読み、飛行中の弾は変えない。</summary>
        [XmlElement, DefaultValue(null)]
        public MSliderReference TimeFuseSlider;

        /// <summary>値を四捨五入してMagazineCapacityに使う(XMLの値より優先)。シミュ開始時に読む。</summary>
        [XmlElement, DefaultValue(null)]
        public MSliderReference MagazineCapacitySlider;

        /// <summary>HoldToFireToggleが無いときの発射方式。true: 押している間連射 / false: 押すたびに連射の開始・停止を切り替え。</summary>
        [XmlElement, DefaultValue(true), Reloadable] public bool HoldToFire = true;

        /// <summary>エフェクトのプレハブ(MuzzleFlash・Projectile/Trail・Projectile/AttachedEffect・Projectile/Explosion/Effect)を探すAssetBundle。
        /// Mac/Linuxでは「バンドル名+Mac」があればそちらを使う。</summary>
        [XmlElement, DefaultValue(null), Reloadable] public ResourceReference AssetBundle;

        [XmlElement, DefaultValue(null), Reloadable] public Vector3 MuzzlePosition = UnityEngine.Vector3.zero;
        [XmlElement, DefaultValue(null), Reloadable] public Vector3 MuzzleRotation = UnityEngine.Vector3.zero;
        [XmlElement, DefaultValue(100f), Reloadable] public float MuzzleVelocity = 100f;
        [XmlElement, DefaultValue(null), Reloadable] public Vector3 EjectVelocity = UnityEngine.Vector3.zero;
        [XmlElement, DefaultValue(0.5f), Reloadable] public float FireInterval = 0.5f;
        [XmlElement, DefaultValue(0f), Reloadable] public float FireDelay = 0f;
        [XmlElement, DefaultValue(0f), Reloadable] public float FireDelayRandomExtra = 0f;
        [XmlElement, DefaultValue(0f), Reloadable] public float Spread = 0f;
        [XmlElement, DefaultValue(1f), Reloadable] public float RecoilMultiplier = 1f;
        [XmlElement, DefaultValue(1), Reloadable] public int BurstCount = 1;
        [XmlElement, DefaultValue(0.1f), Reloadable] public float BurstInterval = 0.1f;

        [XmlElement, DefaultValue(-1), Reloadable] public int TotalAmmo = -1;
        [XmlElement, DefaultValue(0), Reloadable] public int MagazineCapacity = 0;
        [XmlElement, DefaultValue(1f), Reloadable] public float ReloadTime = 1f;
        [XmlElement, DefaultValue(true), Reloadable] public bool AutoReload = true;

        [XmlElement, DefaultValue(false), Reloadable] public bool ShowLoadedProjectile = false;

        /// <summary>AssetBundle内のプレハブ名。</summary>
        [XmlElement, DefaultValue(null), Reloadable] public string MuzzleFlash;
        [XmlElement, DefaultValue(null), Reloadable] public Vector3 MuzzleFlashPosition = UnityEngine.Vector3.zero;
        [XmlElement, DefaultValue(null), Reloadable] public Vector3 MuzzleFlashRotation = UnityEngine.Vector3.zero;
        [XmlElement, DefaultValue(null), Reloadable] public Vector3 MuzzleFlashScale = UnityEngine.Vector3.one;

        [XmlArray("FireSounds"), XmlArrayItem("AudioClip"), DefaultValue(null), CanBeEmpty, Reloadable]
        public List<ResourceReference> FireSounds = new List<ResourceReference>();

        [XmlArray("ReloadSounds"), XmlArrayItem("AudioClip"), DefaultValue(null), CanBeEmpty, Reloadable]
        public List<ResourceReference> ReloadSounds = new List<ResourceReference>();

        [XmlElement, DefaultValue(null), Reloadable] public ProjectileXml Projectile = new ProjectileXml();
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
        [XmlElement, DefaultValue(0.02f)] public float ColliderEnableDelay = 0.02f;

        /// <summary>ブロックのCollidersと同じ書式(SphereCollider / CapsuleCollider / BoxCollider)。</summary>
        [XmlArray("Colliders"), DefaultValue(null), CanBeEmpty]
        [XmlArrayItem("SphereCollider", typeof(SphereModCollider))]
        [XmlArrayItem("CapsuleCollider", typeof(CapsuleModCollider))]
        [XmlArrayItem("BoxCollider", typeof(BoxModCollider))]
        public List<ModCollider> Colliders = new List<ModCollider>();

        [XmlElement, DefaultValue(0.6f)] public float Friction = 0.6f;
        [XmlElement, DefaultValue(0f)] public float Bounciness = 0f;
        [XmlElement, DefaultValue(PhysicMaterialCombine.Average)] public PhysicMaterialCombine FrictionCombine = PhysicMaterialCombine.Average;
        [XmlElement, DefaultValue(PhysicMaterialCombine.Average)] public PhysicMaterialCombine BounceCombine = PhysicMaterialCombine.Average;
        [XmlElement, DefaultValue(50)] public int PoolSize = 50;

        /// <summary>ブロックを定義したModのリソース名(Position/Rotation/ScaleがProjectileSpecのMeshPosition/MeshRotation/MeshScale)。</summary>
        [XmlElement, DefaultValue(null)] public MeshReference Mesh;
        [XmlElement, DefaultValue(null)] public ResourceReference Texture;
        /// <summary>AssetBundle内のプレハブ名。</summary>
        [XmlElement, DefaultValue(null)] public string Trail;
        /// <summary>AssetBundle内のプレハブ名。</summary>
        [XmlElement, DefaultValue(null)] public string AttachedEffect;

        [XmlElement, DefaultValue(10f)] public float ImpactForce = 10f;
        [XmlElement, DefaultValue(0f)] public float BlockDamage = 0f;
        [XmlElement, DefaultValue(0f)] public float EntityDamage = 0f;
        [XmlElement, DefaultValue(false)] public bool StickOnHit = false;
        [XmlElement, DefaultValue(false)] public bool DespawnOnHit = false;

        [XmlElement, DefaultValue(0f)] public float TimeFuse = 0f;
        [XmlElement, DefaultValue(0.05f)] public float TimeFuseRandomExtra = 0.05f;
        [XmlElement, DefaultValue(0f)] public float DetonationDelay = 0f;
        [XmlElement, DefaultValue(0f)] public float DetonationDelayRandomExtra = 0f;

        [XmlElement, DefaultValue(null)] public ExplosionXml Explosion = new ExplosionXml();
        [XmlElement, DefaultValue(null)] public BoosterXml Booster = new BoosterXml();
        /// <summary>誘導(仮実装。要素ごと今後破壊的に変更する予定)。</summary>
        [XmlElement, DefaultValue(null)] public GuidanceXml Guidance = new GuidanceXml();
    }

    [Serializable]
    public class ExplosionXml : Element
    {
        [XmlElement, DefaultValue(0f)] public float Radius = 0f;
        [XmlElement, DefaultValue(0f)] public float Force = 0f;
        [XmlElement, DefaultValue(0f)] public float UpwardsModifier = 0f;
        [XmlElement, DefaultValue(false)] public bool OnlyWithExplodingCannonballs = false;
        /// <summary>AssetBundle内のプレハブ名。</summary>
        [XmlElement, DefaultValue(null)] public string Effect;
        [XmlElement, DefaultValue(false)] public bool FacesHitNormal = false;

        [XmlArray("Sounds"), XmlArrayItem("AudioClip"), DefaultValue(null), CanBeEmpty]
        public List<ResourceReference> Sounds = new List<ResourceReference>();
    }

    [Serializable]
    public class BoosterXml : Element
    {
        [XmlElement, DefaultValue(0f)] public float Thrust = 0f;
        [XmlElement, DefaultValue(0f)] public float BurnTime = 0f;
        [XmlElement, DefaultValue(0f)] public float LateralDrag = 0f;
    }

    [Serializable]
    public class GuidanceXml : Element
    {
        [XmlElement, DefaultValue(0f)] public float Gain = 0f;
    }
}
