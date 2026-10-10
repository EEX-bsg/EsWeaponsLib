using System.Collections.Generic;
using UnityEngine;

namespace EWL.Api
{
	/// <summary>
	/// 武装1基の定義(純データ)。<see cref="Weapon"/> は発射のたびにこの値を読むので、
	/// 発射前に書き換えればその弾から反映される(スライダー連動など)。全項目に既定値あり。
	/// </summary>
	public sealed class WeaponSpec
	{
		public FireSpec Fire = new FireSpec();
		public ProjectileSpec Projectile = new ProjectileSpec();
		public ImpactSpec Impact = new ImpactSpec();
		public EffectSpec Effects = new EffectSpec();
	}

	public sealed class FireSpec
	{
		/// <summary>発射位置(ブロックローカル)。</summary>
		public Vector3 MuzzlePosition = Vector3.zero;
		/// <summary>発射向き(ブロックローカル)。弾はこの回転の +Z へ飛ぶ。</summary>
		public Quaternion MuzzleRotation = Quaternion.identity;
		/// <summary>初速 [m/s]。ブロックの速度に加算される。</summary>
		public float Velocity = 100f;
		/// <summary>最短発射間隔 [s]。</summary>
		public float Interval = 0.5f;
		/// <summary>拡散 [度]。前方に tan(Spread) 半径の球内の乱数ベクトルを足して方向を決める。</summary>
		public float Spread = 0f;
		/// <summary>発射要求から弾が出るまでの固定遅延 [s]。</summary>
		public float Delay = 0f;
		/// <summary>遅延に加える乱数幅 [s](0〜この値)。</summary>
		public float RandomDelay = 0f;
		/// <summary>反動。弾の運動量 × この値の撃力をブロックへ逆向きに与える。</summary>
		public float Recoil = 1f;
		/// <summary>弾数。負値で無制限。マシンの無限弾薬設定時は消費しない。</summary>
		public int Ammo = -1;
	}

	public sealed class ProjectileSpec
	{
		public float Mass = 1f;
		public float Drag = 0f;
		public float AngularDrag = 0f;
		public bool IgnoreGravity = false;
		public CollisionDetectionMode CollisionMode = CollisionDetectionMode.ContinuousDynamic;
		/// <summary>寿命 [s]。着弾しなくてもこの時間で消える。</summary>
		public float Lifetime = 20f;
		/// <summary>時限信管 [s]。0 なら無効。経過で起爆(爆発しない弾は消える)。</summary>
		public float TimeFuse = 0f;
		/// <summary>コライダー(弾ローカル)。空なら半径 0.1 の球。</summary>
		public List<ColliderShape> Colliders = new List<ColliderShape>();
		public float Friction = 0.6f;
		public float Bounciness = 0f;
		public PhysicMaterialCombine FrictionCombine = PhysicMaterialCombine.Average;
		public PhysicMaterialCombine BounceCombine = PhysicMaterialCombine.Average;
		/// <summary>同時に飛べる弾の上限。武装の生成時にこの数まで弾を事前生成し、上限に達したら最古の弾を回収して撃つ。
		/// エフェクトもプレハブごとにこの数まで(超えた分は表示しない)。</summary>
		public int PoolSize = 50;
		/// <summary>弾のプールを共有するキー。同じ種類の武装(同じブロック種別など)で同じ値にすると、弾のオブジェクトを使い回す。
		/// 弾の形(Colliders)はキーごとに最初に作ったもので固定される。null か空文字なら武装ごとの専用プール。
		/// PoolSize は 1〜1000 に丸める。</summary>
		public string PoolKey;
		/// <summary>見た目。null なら球を表示。</summary>
		public Mesh Mesh;
		public Texture Texture;
		public Vector3 MeshPosition = Vector3.zero;
		public Vector3 MeshRotation = Vector3.zero;
		public Vector3 MeshScale = Vector3.one;
	}

	/// <summary>最初の命中で1回だけ適用する。</summary>
	public sealed class ImpactSpec
	{
		public float BlockDamage = 0f;
		public float EntityDamage = 0f;
		/// <summary>命中した剛体に「弾の速度 × この値」の力を与える。</summary>
		public float ImpactForce = 10f;
		/// <summary>命中物に刺さって止まる。</summary>
		public bool Attach = false;
		/// <summary>爆発半径 [m]。0 なら爆発しない。</summary>
		public float ExplosionRadius = 0f;
		/// <summary>AddExplosionForce の力。</summary>
		public float ExplosionPower = 0f;
		/// <summary>AddExplosionForce の上向き補正。</summary>
		public float ExplosionUpPower = 0f;
		/// <summary>命中から起爆までの遅延 [s]。</summary>
		public float FuseDelay = 0f;
		/// <summary>起爆遅延に加える乱数幅 [s](0〜この値)。</summary>
		public float FuseRandom = 0f;
		/// <summary>爆発しない弾を命中時に即消す。false なら命中の1秒後に消える。</summary>
		public bool DespawnOnHit = false;
	}

	/// <summary>エフェクトはプレハブ(AssetBundle 等から取得)、音は AudioClip。null は無し。</summary>
	public sealed class EffectSpec
	{
		public GameObject ShotFlash;
		public Vector3 ShotFlashPosition = Vector3.zero;
		public Vector3 ShotFlashRotation = Vector3.zero;
		public Vector3 ShotFlashScale = Vector3.one;
		/// <summary>弾に追従し、命中・消滅で切り離されてフェードする。</summary>
		public GameObject Trail;
		/// <summary>弾に追従し、弾と一緒に消える。</summary>
		public GameObject Bullet;
		public GameObject Explosion;
		/// <summary>爆発エフェクトを命中面の法線へ向ける。</summary>
		public bool ExplosionFacesNormal = false;
		/// <summary>発射音(ランダムに1つ)。</summary>
		public List<AudioClip> FireSounds = new List<AudioClip>();
		/// <summary>爆発音(ランダムに1つ)。</summary>
		public List<AudioClip> ExplosionSounds = new List<AudioClip>();
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
