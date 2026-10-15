using UnityEngine;

namespace EWL.Api
{
    /// <summary>
    /// 弾の命中1回分(<see cref="Weapon.Hit"/> / <see cref="Weapon.AnyHit"/> の引数)。直撃は1回、爆風は剛体ごとに1回。
    /// 撃った側の情報は発射時点のもの。どの武器の弾かは <see cref="Source"/>(<see cref="Weapon.AnyHit"/> はバニラの一部の武装も含む)。
    /// </summary>
    public sealed class ProjectileHit
    {
        internal ProjectileHit() { }

        /// <summary>どの武器の弾か。今後種類が増えうるので、知らない値は無視する。</summary>
        public HitSource Source { get; internal set; }

        /// <summary>撃った武装(EWLの弾だけ。バニラの弾はnull)。弾が飛んでいる間に破棄されていれば <see cref="Api.Weapon.IsAlive"/> がfalse。</summary>
        public Weapon Weapon { get; internal set; }

        /// <summary>撃ったブロック(シミュ用。EWLの弾だけで、バニラの弾はnull)。壊れた・シミュが終わった後はnull。</summary>
        public BlockBehaviour ShooterBlock { get; internal set; }

        /// <summary>撃った人(発射時点の値。撃ったブロックが消えても残る)。バニラの弾で分からないときはnull。</summary>
        public Attacker Shooter { get; internal set; }

        /// <summary>当たったブロック(子ブロックを持つブロックは当たった子)。地形・エンティティ等はnull。</summary>
        public BlockBehaviour Block { get; internal set; }

        /// <summary>当たったコライダー(直撃のみ。爆風と、当たったコライダーが分からないバニラの弾はnull)。</summary>
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

    /// <summary>どの武器の弾か。</summary>
    public enum HitSource
    {
        /// <summary>EWLの武装(XMLカスタムモジュール・C# API。EWLを使う他Modのものを含む)。</summary>
        Ewl,
        /// <summary>Besiege標準の大砲の砲弾(散弾砲は含まない)。ゴッドツール「爆発する砲弾」の爆風を含む。</summary>
        VanillaCannon,
        /// <summary>Besiege標準のクロスボウの矢。</summary>
        VanillaCrossbow,
        /// <summary>Besiege標準のFuelCannonの火球。当たった相手はEWLが本体と同じ判定をやり直して求めるので、まれに本体とずれることがある。</summary>
        VanillaFuelCannon,
    }
}
