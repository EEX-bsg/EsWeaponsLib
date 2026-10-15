using UnityEngine;

namespace EWL.Api
{
    /// <summary>
    /// EWL の弾の命中1回分(<see cref="Weapon.Hit"/> / <see cref="Weapon.AnyHit"/> の引数)。直撃は1回、爆風は剛体ごとに1回。
    /// 撃った側の情報は発射時点のもの。
    /// </summary>
    public sealed class ProjectileHit
    {
        internal ProjectileHit() { }

        /// <summary>撃った武装。弾が飛んでいる間に破棄されていれば <see cref="Api.Weapon.IsAlive"/> が false。</summary>
        public Weapon Weapon { get; internal set; }

        /// <summary>撃ったブロック(シミュ用)。壊れた・シミュが終わった後は null。</summary>
        public BlockBehaviour ShooterBlock { get; internal set; }

        /// <summary>撃った人(発射時点の値。撃ったブロックが消えても残る)。</summary>
        public Attacker Shooter { get; internal set; }

        /// <summary>当たったブロック(子ブロックを持つブロックは当たった子)。地形・エンティティ等は null。</summary>
        public BlockBehaviour Block { get; internal set; }

        /// <summary>当たったコライダー(直撃のみ。爆風は null)。</summary>
        public Collider Collider { get; internal set; }

        /// <summary>直撃は接触点、爆風は爆発の中心(ワールド座標)。</summary>
        public Vector3 Point { get; internal set; }

        /// <summary>直撃か爆風か。今後種類が増えうるので、知らない値は無視する。</summary>
        public HitKind Kind { get; internal set; }
    }

    /// <summary>命中の種類。</summary>
    public enum HitKind
    {
        /// <summary>弾が直接当たった。</summary>
        Direct,
        /// <summary>爆風が当たった。</summary>
        Explosion,
    }
}
