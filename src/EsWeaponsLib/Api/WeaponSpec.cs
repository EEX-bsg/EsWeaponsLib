using System.Collections.Generic;
using UnityEngine;

namespace EWL.Api
{
    /// <summary>
    /// 武装1基の定義(純データ)。発射機そのものの値と、撃つ弾の定義 <see cref="Projectile"/> を持つ。
    /// <see cref="Weapon"/> は発射のたびにこの値を読むので、発射前に書き換えればその弾から反映される(スライダー連動など)。
    /// 全項目に既定値あり。位置・回転はブロックローカルで、回転はオイラー角 [度]。
    /// 中の定義(Projectile・Explosion・Booster・Guidance)にnullを代入しない(使わない機能は既定値のまま = 無効)。
    /// </summary>
    public sealed class WeaponSpec
    {
        /// <summary>発射位置。</summary>
        public Vector3 MuzzlePosition = Vector3.zero;
        /// <summary>発射向き(オイラー角 [度])。弾はこの回転の +Zへ飛ぶ。</summary>
        public Vector3 MuzzleRotation = Vector3.zero;
        /// <summary>初速 [m/s]。ブロックの速度に加算される。</summary>
        public float MuzzleVelocity = 100f;
        /// <summary>発射時に弾のローカル軸で足す速度 [m/s](ロケットを発射筒から押し出す等)。反動には含めない。</summary>
        public Vector3 EjectVelocity = Vector3.zero;
        /// <summary>最短発射間隔 [s]。</summary>
        public float FireInterval = 0.5f;
        /// <summary>発射要求から弾が出るまでの固定遅延 [s]。</summary>
        public float FireDelay = 0f;
        /// <summary>遅延に加える乱数幅 [s](0〜この値)。</summary>
        public float FireDelayRandomExtra = 0f;
        /// <summary>拡散 [度]。前方にtan(Spread) 半径の球内の乱数ベクトルを足して方向を決める。弾の向きもこの方向に合わせる。</summary>
        public float Spread = 0f;
        /// <summary>反動の倍率。弾の運動量 × この値の撃力をブロックへ逆向きに与える(1で物理的に正しい反動)。</summary>
        public float RecoilMultiplier = 1f;
        /// <summary>1回の発射要求で撃つ弾数。1発ごとに弾数を消費し、弾切れで止まる。</summary>
        public int BurstCount = 1;
        /// <summary>バースト内の弾の間隔 [s]。次の発射要求は最後の弾からFireInterval後に受け付ける。</summary>
        public float BurstInterval = 0.1f;

        /// <summary>総弾数(弾倉内を含む)。負値で無制限。マシンの無限弾薬設定時は消費しない
        /// (弾倉を使う武装は弾倉の弾だけ減り、予備弾は減らない)。武装の生成時だけ読む。</summary>
        public int TotalAmmo = -1;
        /// <summary>弾倉の容量。0以下なら弾倉なし。武装の生成時にTotalAmmoから弾倉を満たし、残りを予備弾にする。</summary>
        public int MagazineCapacity = 0;
        /// <summary>リロード時間 [s]。リロード中は撃てない。</summary>
        public float ReloadTime = 1f;
        /// <summary>弾倉が空になったら自動でリロードする。</summary>
        public bool AutoReload = true;

        /// <summary>撃てる状態の間、次の弾の見た目を発射位置に表示する(迫撃砲・爆弾など)。発射の瞬間に消え、発射間隔の後に戻る。
        /// 弾切れ・リロード中は表示しない。見た目だけでコライダーは持たない。
        /// EwlWeapon モジュールでは、総弾数が 0 でなければビルド中も表示する(C# から武装を作る場合は <see cref="Weapon.CreateLoadedProjectilePreview"/>)。</summary>
        public bool ShowLoadedProjectile = false;

        /// <summary>発射エフェクトのプレハブ(AssetBundle等から取得)。nullなら無し。</summary>
        public GameObject MuzzleFlash;
        /// <summary>発射エフェクトの位置(ブロックローカル)。発射音もここで鳴る。</summary>
        public Vector3 MuzzleFlashPosition = Vector3.zero;
        /// <summary>発射エフェクトの向き(オイラー角 [度]、ブロックローカル)。</summary>
        public Vector3 MuzzleFlashRotation = Vector3.zero;
        /// <summary>発射エフェクトの拡大率。</summary>
        public Vector3 MuzzleFlashScale = Vector3.one;
        /// <summary>発射音(ランダムに1つ。MuzzleFlashPositionで鳴る)。</summary>
        public List<AudioClip> FireSounds = new List<AudioClip>();
        /// <summary>リロード開始時の音(ランダムに1つ)。</summary>
        public List<AudioClip> ReloadSounds = new List<AudioClip>();

        /// <summary>撃つ弾。飛行中の弾は発射時のProjectileSpecを使い続けるので、差し替えは次の弾から効く
        /// (同じインスタンスの値を書き換えた場合は飛行中の弾にも効く)。弾の形は <see cref="ProjectileSpec.PoolKey"/> ごとに固定され、
        /// 差し替えた先の弾は事前生成されず最初の発射時に作られる。</summary>
        public ProjectileSpec Projectile = new ProjectileSpec();
    }

    /// <summary>
    /// 弾1種類の定義(純データ)。弾体・命中・信管・見た目と、爆発・推進・誘導を持つ。
    /// 複数の武装で同じインスタンスを共有してよい(書き換えると共有している全武装に効く)。
    /// </summary>
    public sealed class ProjectileSpec
    {
        /// <summary>弾の質量 [kg]。反動・推進の加速度と、物理的な衝突に効く(命中時に与える力はImpactForceで決まる)。</summary>
        public float Mass = 1f;
        /// <summary>空気抵抗(Rigidbody.drag)。</summary>
        public float Drag = 0f;
        /// <summary>回転の抵抗(Rigidbody.angularDrag)。</summary>
        public float AngularDrag = 0f;
        /// <summary>trueなら重力を受けない。</summary>
        public bool IgnoreGravity = false;
        /// <summary>衝突検出の方式。速い弾がすり抜けないよう、既定はContinuousDynamic。</summary>
        public CollisionDetectionMode CollisionMode = CollisionDetectionMode.ContinuousDynamic;
        /// <summary>寿命 [s]。着弾しなくてもこの時間で消える。</summary>
        public float Lifetime = 20f;
        /// <summary>発射後、弾のコライダーを有効にするまでの時間 [s]。自機との干渉を防ぐ。初速の遅い弾(ロケット・ミサイル・爆弾等)は
        /// 長めにする。0なら発射と同時に有効(負値は0扱い)。撃った武装ブロックとはこの値に関係なく衝突しない。</summary>
        public float ColliderEnableDelay = 0.02f;
        /// <summary>コライダー(弾ローカル)。空なら半径0.1の球。</summary>
        public List<ColliderShape> Colliders = new List<ColliderShape>();
        /// <summary>弾の摩擦係数(静止・動摩擦とも)。</summary>
        public float Friction = 0.6f;
        /// <summary>弾の反発係数(0〜1)。</summary>
        public float Bounciness = 0f;
        /// <summary>相手の摩擦との合成方法。</summary>
        public PhysicMaterialCombine FrictionCombine = PhysicMaterialCombine.Average;
        /// <summary>相手の反発との合成方法。</summary>
        public PhysicMaterialCombine BounceCombine = PhysicMaterialCombine.Average;
        /// <summary>同時に飛べる弾の上限。武装の生成時にこの数まで弾を事前生成し、上限に達したら最古の弾を回収して撃つ。
        /// エフェクトもプレハブごとにこの数まで(超えた分は表示しない)。1〜1000に丸める。</summary>
        public int PoolSize = 50;
        /// <summary>弾のプールを共有するキー。同じ種類の弾で同じ値にすると、弾のオブジェクトを使い回す。
        /// 弾の形(Colliders)はキーごとに最初に作ったもので固定される。nullか空文字なら武装ごとの専用プール(実際のキーは <see cref="Weapon.PoolKey"/>)。</summary>
        public string PoolKey;

        /// <summary>見た目。nullなら球を表示。</summary>
        public Mesh Mesh;
        /// <summary>見た目のテクスチャ。nullならテクスチャなし(白)。</summary>
        public Texture Texture;
        /// <summary>見た目の位置(弾ローカル)。Meshがnullのときは使わない。</summary>
        public Vector3 MeshPosition = Vector3.zero;
        /// <summary>見た目の向き(オイラー角 [度]、弾ローカル)。Meshがnullのときは使わない。</summary>
        public Vector3 MeshRotation = Vector3.zero;
        /// <summary>見た目の拡大率。Meshがnullのときは使わない。</summary>
        public Vector3 MeshScale = Vector3.one;
        /// <summary>弾に追従し、命中・消滅で切り離されてフェードするエフェクトのプレハブ。</summary>
        public GameObject Trail;
        /// <summary>弾に追従し、弾と一緒に消えるエフェクトのプレハブ(弾の光・炎など)。</summary>
        public GameObject AttachedEffect;

        /// <summary>命中した剛体に「弾の速度 × この値」の力 [N/(m/s)] を与える。ダメージ・力は最初の命中で1回だけ。</summary>
        public float ImpactForce = 10f;
        /// <summary>直撃したとき、HPを持つブロックに与えるダメージ(爆風では使わない)。</summary>
        public float BlockDamage = 0f;
        /// <summary>直撃したとき、レベルのエンティティ(人・動物など)に与えるダメージ(爆風では使わない)。</summary>
        public float EntityDamage = 0f;
        /// <summary>命中物にくっついて止まり、以後は命中物と一緒に動く(矢など)。地形では、その場で止まる。</summary>
        public bool AttachOnHit = false;
        /// <summary>爆発しない弾を命中時に即消す。falseなら命中の1秒後に消える。</summary>
        public bool DespawnOnHit = false;

        /// <summary>時限信管 [s]。発射からこの時間で起爆(爆発しない弾は消える)。0は発射直後に起爆。既定は無限(時限信管なし)。</summary>
        public float TimeFuse = float.PositiveInfinity;
        /// <summary>時限信管に加える乱数 [s](0〜この値)。発射時に決める。TimeFuseが無限のときは効かない。</summary>
        public float TimeFuseRandomExtra = 0.05f;
        /// <summary>命中から起爆までの遅延 [s]。</summary>
        public float DetonationDelay = 0f;
        /// <summary>起爆遅延に加える乱数幅 [s](0〜この値)。</summary>
        public float DetonationDelayRandomExtra = 0f;

        /// <summary>起爆したときの爆発。Radiusが0なら爆発しない。</summary>
        public ExplosionSpec Explosion = new ExplosionSpec();
        /// <summary>推進(ロケット)。Thrustが0なら推進しない。</summary>
        public BoosterSpec Booster = new BoosterSpec();
        /// <summary>誘導(仮実装)。Gainが0なら誘導しない。</summary>
        public GuidanceSpec Guidance = new GuidanceSpec();
    }

    /// <summary>爆発1回の定義(純データ)。範囲内の剛体にAddExplosionForceと本体の爆発処理を与える。</summary>
    public sealed class ExplosionSpec
    {
        /// <summary>半径 [m]。0なら爆発しない。</summary>
        public float Radius = 0f;
        /// <summary>AddExplosionForceのexplosionForce。</summary>
        public float Force = 0f;
        /// <summary>AddExplosionForceのupwardsModifier。</summary>
        public float UpwardsModifier = 0f;
        /// <summary>通常は爆発しない弾として扱い、ゴッドツール「爆発する砲弾」が有効なときだけ爆発する。</summary>
        public bool OnlyWithExplodingCannonballs = false;
        /// <summary>爆発エフェクトのプレハブ。nullなら無し。</summary>
        public GameObject Effect;
        /// <summary>爆発エフェクトを命中面の法線へ向ける。</summary>
        public bool FacesHitNormal = false;
        /// <summary>爆発音(ランダムに1つ)。</summary>
        public List<AudioClip> Sounds = new List<AudioClip>();
    }

    /// <summary>弾の推進(ロケット)と安定翼。Thrustが0なら推進しない。命中したら推力・安定翼とも止まる。</summary>
    public sealed class BoosterSpec
    {
        /// <summary>推力 [N]。弾の前方へ毎物理フレーム加える(加速度 = Thrust / 弾のMass)。</summary>
        public float Thrust = 0f;
        /// <summary>推力を出す時間 [s]。発射からこの時間で推力が止まる。0は推力なし。既定は無限(弾が消えるまで)。</summary>
        public float ThrustTime = float.PositiveInfinity;
        /// <summary>
        /// 風見安定(安定翼)の強さ [(rad/s²)/(m/s)]。機首に垂直な速度成分 × この値の角加速度で、機首を飛ぶ向きへ寄せる
        /// (弾の質量・形に依らない)。横滑りが大きいほど強く戻り、重力で曲がる弾道にも機首が沿う。
        /// 推力の有無・燃焼の終了に関係なく、命中まで飛行中ずっと働く。0なら働かない。
        /// 速度そのものは変えないので、横滑りは機首が向き直って推力が掛かることで消える。
        /// 振動を抑えるには<see cref="ProjectileSpec.AngularDrag"/>を併用する。
        /// </summary>
        public float Stability = 0f;
    }

    /// <summary>
    /// 誘導(仮実装)。この型・XMLのGuidance要素を含め、目標の決め方・操舵とも今後破壊的に変更する予定。
    /// 発射の瞬間に、発射方向の前方一定角度内にいる敵マシンの
    /// コアブロックのうち最も近いものを目標にし、飛行中は変えない。目標が無ければ発射方向を保って飛ぶ。
    /// 弾の向きをトルクで変えるだけなので、推進(<see cref="BoosterSpec"/>)と組み合わせて使う。
    /// </summary>
    public sealed class GuidanceSpec
    {
        /// <summary>誘導の強さ(旋回トルクの係数)。0なら誘導しない。</summary>
        public float Gain = 0f;
    }

    /// <summary>弾のコライダーの形。</summary>
    public enum ColliderKind { Sphere, Capsule, Box }

    /// <summary>弾のコライダー1個。SizeはBoxの大きさ、RadiusはSphere/Capsule用、Height/Direction(0=X,1=Y,2=Z)はCapsule用。</summary>
    public sealed class ColliderShape
    {
        /// <summary>形。</summary>
        public ColliderKind Kind = ColliderKind.Sphere;
        /// <summary>位置(弾ローカル)。</summary>
        public Vector3 Position = Vector3.zero;
        /// <summary>向き(オイラー角 [度]、弾ローカル)。</summary>
        public Vector3 Rotation = Vector3.zero;
        /// <summary>Boxの大きさ [m]。</summary>
        public Vector3 Size = Vector3.one;
        /// <summary>Sphere・Capsuleの半径 [m]。</summary>
        public float Radius = 0.1f;
        /// <summary>Capsuleの高さ [m]。</summary>
        public float Height = 1f;
        /// <summary>Capsuleの向きの軸(0 = X、1 = Y、2 = Z)。</summary>
        public int Direction = 2;
    }
}
