using System.Collections.Generic;
using UnityEngine;

namespace EWL.Api
{
    /// <summary>
    /// 武装1基の定義(純データ)。発射機そのものの値と、撃つ弾の定義 <see cref="Projectile"/> を持つ。
    /// <see cref="Weapon"/> は発射のたびにこの値を読むので、発射前に書き換えればその弾から反映される(スライダー連動など)。
    /// 全項目に既定値あり。位置・回転はブロックローカルで、回転はオイラー角 [度]。
    /// 中の定義(Projectile・Explosion・Booster・Guidance)に null を代入しない(使わない機能は既定値のまま = 無効)。
    /// </summary>
    public sealed class WeaponSpec
    {
        /// <summary>発射位置。</summary>
        public Vector3 MuzzlePosition = Vector3.zero;
        /// <summary>発射向き(オイラー角 [度])。弾はこの回転の +Z へ飛ぶ。</summary>
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
        /// <summary>拡散 [度]。前方に tan(Spread) 半径の球内の乱数ベクトルを足して方向を決める。弾の向きもこの方向に合わせる。</summary>
        public float Spread = 0f;
        /// <summary>反動の倍率。弾の運動量 × この値の撃力をブロックへ逆向きに与える(1 で物理的に正しい反動)。</summary>
        public float RecoilMultiplier = 1f;
        /// <summary>1回の発射要求で撃つ弾数。1発ごとに弾数を消費し、弾切れで止まる。</summary>
        public int BurstCount = 1;
        /// <summary>バースト内の弾の間隔 [s]。次の発射要求は最後の弾から FireInterval 後に受け付ける。</summary>
        public float BurstInterval = 0.1f;

        /// <summary>総弾数(弾倉内を含む)。負値で無制限。マシンの無限弾薬設定時は消費しない
        /// (弾倉を使う武装は弾倉の弾だけ減り、予備弾は減らない)。武装の生成時だけ読む。</summary>
        public int TotalAmmo = -1;
        /// <summary>弾倉の容量。0 以下なら弾倉なし。武装の生成時に TotalAmmo から弾倉を満たし、残りを予備弾にする。</summary>
        public int MagazineCapacity = 0;
        /// <summary>リロード時間 [s]。リロード中は撃てない。</summary>
        public float ReloadTime = 1f;
        /// <summary>弾倉が空になったら自動でリロードする。</summary>
        public bool AutoReload = true;

        /// <summary>撃てる状態の間、次の弾の見た目を発射位置に表示する(迫撃砲・爆弾など)。発射の瞬間に消え、発射間隔の後に戻る。
        /// 弾切れ・リロード中は表示しない。見た目だけでコライダーは持たない。</summary>
        public bool ShowLoadedProjectile = false;

        /// <summary>発射エフェクトのプレハブ(AssetBundle 等から取得)。null なら無し。</summary>
        public GameObject MuzzleFlash;
        public Vector3 MuzzleFlashPosition = Vector3.zero;
        public Vector3 MuzzleFlashRotation = Vector3.zero;
        public Vector3 MuzzleFlashScale = Vector3.one;
        /// <summary>発射音(ランダムに1つ。MuzzleFlashPosition で鳴る)。</summary>
        public List<AudioClip> FireSounds = new List<AudioClip>();
        /// <summary>リロード開始時の音(ランダムに1つ)。</summary>
        public List<AudioClip> ReloadSounds = new List<AudioClip>();

        /// <summary>撃つ弾。飛行中の弾は発射時の ProjectileSpec を使い続けるので、差し替えは次の弾から効く
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
        public float Mass = 1f;
        public float Drag = 0f;
        public float AngularDrag = 0f;
        public bool IgnoreGravity = false;
        public CollisionDetectionMode CollisionMode = CollisionDetectionMode.ContinuousDynamic;
        /// <summary>寿命 [s]。着弾しなくてもこの時間で消える。</summary>
        public float Lifetime = 20f;
        /// <summary>発射後、弾のコライダーを有効にするまでの時間 [s]。自機との干渉を防ぐ。初速の遅い弾(ロケット・ミサイル・爆弾等)は
        /// 長めにする。0 なら発射と同時に有効(負値は 0 扱い)。撃った武装ブロックとはこの値に関係なく衝突しない。</summary>
        public float ColliderEnableDelay = 0.02f;
        /// <summary>コライダー(弾ローカル)。空なら半径 0.1 の球。</summary>
        public List<ColliderShape> Colliders = new List<ColliderShape>();
        public float Friction = 0.6f;
        public float Bounciness = 0f;
        public PhysicMaterialCombine FrictionCombine = PhysicMaterialCombine.Average;
        public PhysicMaterialCombine BounceCombine = PhysicMaterialCombine.Average;
        /// <summary>同時に飛べる弾の上限。武装の生成時にこの数まで弾を事前生成し、上限に達したら最古の弾を回収して撃つ。
        /// エフェクトもプレハブごとにこの数まで(超えた分は表示しない)。1〜1000 に丸める。</summary>
        public int PoolSize = 50;
        /// <summary>弾のプールを共有するキー。同じ種類の弾で同じ値にすると、弾のオブジェクトを使い回す。
        /// 弾の形(Colliders)はキーごとに最初に作ったもので固定される。null か空文字なら武装ごとの専用プール(実際のキーは <see cref="Weapon.PoolKey"/>)。</summary>
        public string PoolKey;

        /// <summary>見た目。null なら球を表示。</summary>
        public Mesh Mesh;
        public Texture Texture;
        public Vector3 MeshPosition = Vector3.zero;
        public Vector3 MeshRotation = Vector3.zero;
        public Vector3 MeshScale = Vector3.one;
        /// <summary>弾に追従し、命中・消滅で切り離されてフェードするエフェクトのプレハブ。</summary>
        public GameObject Trail;
        /// <summary>弾に追従し、弾と一緒に消えるエフェクトのプレハブ(弾の光・炎など)。</summary>
        public GameObject AttachedEffect;

        /// <summary>命中した剛体に「弾の速度 × この値」の力 [N/(m/s)] を与える。ダメージ・力は最初の命中で1回だけ。</summary>
        public float ImpactForce = 10f;
        public float BlockDamage = 0f;
        public float EntityDamage = 0f;
        /// <summary>命中物に刺さって止まる。</summary>
        public bool StickOnHit = false;
        /// <summary>爆発しない弾を命中時に即消す。false なら命中の1秒後に消える。</summary>
        public bool DespawnOnHit = false;

        /// <summary>時限信管 [s]。0 なら無効。経過で起爆(爆発しない弾は消える)。</summary>
        public float TimeFuse = 0f;
        /// <summary>時限信管に加える乱数 [s](0〜この値)。発射時に決める。TimeFuse が 0 のときは使わない。</summary>
        public float TimeFuseRandomExtra = 0.05f;
        /// <summary>命中から起爆までの遅延 [s]。</summary>
        public float DetonationDelay = 0f;
        /// <summary>起爆遅延に加える乱数幅 [s](0〜この値)。</summary>
        public float DetonationDelayRandomExtra = 0f;

        /// <summary>起爆したときの爆発。Radius が 0 なら爆発しない。</summary>
        public ExplosionSpec Explosion = new ExplosionSpec();
        /// <summary>推進(ロケット)。Thrust が 0 なら推進しない。</summary>
        public BoosterSpec Booster = new BoosterSpec();
        /// <summary>誘導(仮実装)。Gain が 0 なら誘導しない。</summary>
        public GuidanceSpec Guidance = new GuidanceSpec();
    }

    /// <summary>爆発1回の定義(純データ)。範囲内の剛体に AddExplosionForce と本体の爆発処理を与える。</summary>
    public sealed class ExplosionSpec
    {
        /// <summary>半径 [m]。0 なら爆発しない。</summary>
        public float Radius = 0f;
        /// <summary>AddExplosionForce の explosionForce。</summary>
        public float Force = 0f;
        /// <summary>AddExplosionForce の upwardsModifier。</summary>
        public float UpwardsModifier = 0f;
        /// <summary>通常は爆発しない弾として扱い、ゴッドツール「爆発する砲弾」が有効なときだけ爆発する。</summary>
        public bool OnlyWithExplodingCannonballs = false;
        /// <summary>爆発エフェクトのプレハブ。null なら無し。</summary>
        public GameObject Effect;
        /// <summary>爆発エフェクトを命中面の法線へ向ける。</summary>
        public bool FacesHitNormal = false;
        /// <summary>爆発音(ランダムに1つ)。</summary>
        public List<AudioClip> Sounds = new List<AudioClip>();
    }

    /// <summary>弾の推進(ロケット)。Thrust が 0 なら推進しない。命中したら推力は止まる。</summary>
    public sealed class BoosterSpec
    {
        /// <summary>推力 [N]。弾の前方へ毎物理フレーム加える(加速度 = Thrust / 弾の Mass)。</summary>
        public float Thrust = 0f;
        /// <summary>燃焼時間 [s]。0 なら弾が消えるまで燃え続ける。</summary>
        public float BurnTime = 0f;
        /// <summary>燃焼中、弾の向きに対する横滑り速度 × この値の力で横滑りを抑える(安定翼)[N/(m/s)]。</summary>
        public float LateralDrag = 0f;
    }

    /// <summary>
    /// 誘導(仮実装)。この型・XML の Guidance 要素を含め、目標の決め方・操舵とも今後破壊的に変更する予定。
    /// 発射の瞬間に、発射方向の前方一定角度内にいる敵マシンの
    /// コアブロックのうち最も近いものを目標にし、飛行中は変えない。目標が無ければ発射方向を保って飛ぶ。
    /// 弾の向きをトルクで変えるだけなので、推進(<see cref="BoosterSpec"/>)と組み合わせて使う。
    /// </summary>
    public sealed class GuidanceSpec
    {
        /// <summary>誘導の強さ(旋回トルクの係数)。0 なら誘導しない。</summary>
        public float Gain = 0f;
    }

    public enum ColliderKind { Sphere, Capsule, Box }

    /// <summary>弾のコライダー1個。Size は Box の大きさ、Radius/Height/Direction(0=X,1=Y,2=Z)は Sphere/Capsule 用。</summary>
    public sealed class ColliderShape
    {
        public ColliderKind Kind = ColliderKind.Sphere;
        public Vector3 Position = Vector3.zero;
        public Vector3 Rotation = Vector3.zero;
        public Vector3 Size = Vector3.one;
        public float Radius = 0.1f;
        public float Height = 1f;
        public int Direction = 2;
    }
}
