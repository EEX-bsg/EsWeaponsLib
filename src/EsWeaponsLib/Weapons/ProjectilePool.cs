using System.Collections.Generic;
using EWL.Api;
using UnityEngine;

namespace EWL.Weapons
{
    // 弾のプール(ACMu と同じ設計)。キーごとに待機キューと「同時に飛べる数」の上限を持つ。
    //  1) 待機中があれば再利用 2) 上限未満なら生成 3) 上限なら同じキーの最古の弾を回収して再利用(発射は止めない)。
    // 武装の生成時に上限数まで事前生成する。弾は待機中も飛行中もキーごとの置き場の子(刺さったときだけ刺さった先の子)。
    // 置き場は SceneRoot の下。待機中の弾はシミュをまたいで再利用し、シーンが変わったら全部捨てる。飛行中の弾はシミュ終了で戻る。
    // 弾の形(コライダー)はキーごとに最初に組み立てたもので固定。質量・見た目・物理マテリアルは発射ごとに設定し直す。
    internal static class ProjectilePool
    {
        internal sealed class Pool
        {
            internal readonly Queue<Projectile> Idle = new Queue<Projectile>();
            internal readonly List<Projectile> Active = new List<Projectile>(); // 発射順
            internal int Total;
            internal int MaxCount;
            internal Transform Root;
            internal bool Discarded;
        }

        private const int MaxPoolSize = 1000; // XML の書き間違いでシミュ開始時に大量生成して固まるのを防ぐ

        private static readonly Dictionary<string, Pool> Pools = new Dictionary<string, Pool>();
        private static Transform _poolRoot;

        /// <summary>上限数まで事前生成する(戦闘中の生成を避ける)。上限は同じキーの武装の最大値。</summary>
        internal static void Prewarm(string key, ProjectileSpec spec)
        {
            var pool = GetPool(key, spec.PoolSize);
            while (pool.Total < pool.MaxCount) pool.Idle.Enqueue(Create(pool, spec));
        }

        internal static Projectile Acquire(string key, ProjectileSpec spec)
        {
            var pool = GetPool(key, spec.PoolSize);
            DropDestroyedIdle(pool);
            Projectile projectile;
            if (pool.Idle.Count > 0) projectile = pool.Idle.Dequeue();
            else if (pool.Total < pool.MaxCount || pool.Active.Count == 0) projectile = Create(pool, spec);
            else
            {
                pool.Active[0].Despawn(); // 上限: 最古の弾を回収して使う
                projectile = pool.Idle.Count > 0 ? pool.Idle.Dequeue() : Create(pool, spec);
            }
            pool.Active.Add(projectile);
            return projectile;
        }

        internal static void Release(Projectile projectile)
        {
            var pool = projectile.Home;
            pool.Active.Remove(projectile);
            if (pool.Discarded)
            {
                Object.Destroy(projectile.gameObject);
                DestroyRootIfEmpty(pool);
                return;
            }
            projectile.gameObject.SetActive(false);
            projectile.transform.SetParent(pool.Root, false);
            projectile.transform.localScale = Vector3.one; // 刺さったときの補正を戻す
            pool.Idle.Enqueue(projectile);
        }

        /// <summary>使用中に親(刺さった先)ごと破棄されたとき。</summary>
        internal static void Forget(Projectile projectile)
        {
            var pool = projectile.Home;
            if (pool == null || !pool.Active.Remove(projectile)) return;
            pool.Total--;
            if (pool.Discarded) DestroyRootIfEmpty(pool);
        }

        /// <summary>武装専用のキー(PoolKey 未指定)のプールを、武装の破棄時に捨てる。
        /// 飛行中の弾も置き場の子なので、置き場は最後の弾が戻ったときに消す。</summary>
        internal static void Discard(string key)
        {
            Pool pool;
            if (!Pools.TryGetValue(key, out pool)) return;
            Pools.Remove(key);
            pool.Discarded = true;
            foreach (var idle in pool.Idle) if (idle != null) Object.Destroy(idle.gameObject); // 終了時は破棄済みのことがある
            pool.Idle.Clear();
            DestroyRootIfEmpty(pool);
        }

        private static void DestroyRootIfEmpty(Pool pool)
        {
            if (pool.Active.Count == 0 && pool.Root != null) Object.Destroy(pool.Root.gameObject);
        }

        private static Pool GetPool(string key, int maxCount)
        {
            var poolRoot = PoolRoot();
            Pool pool;
            if (!Pools.TryGetValue(key, out pool))
            {
                pool = new Pool();
                pool.Root = new GameObject(key).transform;
                pool.Root.SetParent(poolRoot, false);
                Pools[key] = pool;
            }
            maxCount = Mathf.Clamp(maxCount, 1, MaxPoolSize);
            if (maxCount > pool.MaxCount) pool.MaxCount = maxCount;
            return pool;
        }

        // 通常は起きないが、外部から破棄されたものは数から外す。
        private static void DropDestroyedIdle(Pool pool)
        {
            while (pool.Idle.Count > 0 && pool.Idle.Peek() == null)
            {
                pool.Idle.Dequeue();
                pool.Total--;
            }
        }

        private static Projectile Create(Pool pool, ProjectileSpec spec)
        {
            var projectile = Projectile.Build(pool.Root, spec);
            projectile.Home = pool;
            pool.Total++;
            return projectile;
        }

        // 置き場が消えていたらシーンが変わった: 中の弾も全部消えているので、プールを一から作り直す。
        private static Transform PoolRoot()
        {
            if (_poolRoot != null) return _poolRoot;
            Pools.Clear();
            _poolRoot = SceneRoot.CreateChild("ProjectilePool");
            return _poolRoot;
        }
    }
}
