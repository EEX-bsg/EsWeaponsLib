using System.Collections.Generic;
using EWL.Api;
using UnityEngine;

namespace EWL.Besiege
{
    // バニラの弾(大砲・クロスボウ・FuelCannon)と爆発する砲弾の爆風の命中を Weapon.AnyHit に流す仕組みの入口。
    // 本体は命中に加害者を渡さないので、弾の元オブジェクト(ブロックが Instantiate する見本と、MP ホストの弾のプール)に監視部品を付け、
    // 複製される弾に乗せる。矢の弾倉はシミュ用ブロックの Awake で作られる(OnBlockInit より前)ので、
    // 見本にはビルド用ブロックとブロックの原本(PrefabMaster)の時点で付けておく。
    internal static class VanillaProjectiles
    {
        private static bool _blockPrefabsPatched;
        private static ProjectileManager _patchedManager;
        private static readonly HashSet<int> PatchedPoolTypes = new HashSet<int>();

        /// <summary>見本に部品を付けている最中(付けた部品の Awake は見本のものなので動かさない)。</summary>
        internal static bool Patching { get; private set; }

        internal static bool Listening { get { return Weapon.HasHitListeners(null); } }

        /// <summary>Events.OnBlockInit から。ビルド用・シミュ用の両方に付ける(シミュ用はビルド用の複製)。</summary>
        internal static void OnBlockInit(Modding.Blocks.Block block)
        {
            // 全ブロック(他 Mod を含む)の初期化で呼ばれるので、例外を他の購読者へ漏らさない
            try
            {
                Patching = true;
                PatchBlockPrefabs();
                PatchBlock(block.InternalObject);
            }
            catch (System.Exception ex) { Debug.LogError("[EWL] VanillaProjectiles.OnBlockInit failed: " + ex); }
            finally { Patching = false; }
        }

        internal static void RaiseHit(HitSource source, Attacker shooter, BlockBehaviour block, Collider collider, Vector3 point, HitKind kind)
        {
            Weapon.RaiseHit(new ProjectileHit
            {
                Source = source,
                Shooter = shooter,
                Block = block,
                Collider = collider,
                Point = point,
                Kind = kind,
            });
        }

        /// <summary>弾を撃ったマシン(本体の砲弾と同じ取り方)。MP のグローバルシミュではプールの弾の持ち主の番号から、それ以外は自分のマシン。</summary>
        internal static Machine ShooterMachine(GameObject projectile)
        {
            var pooled = StatMaster.isMP && !StatMaster.isLocalSim ? projectile.GetComponent<NetworkProjectile>() : null;
            ServerMachine machine;
            if (pooled != null && NetworkScene.Instance != null && NetworkScene.Instance.GetMachine(pooled.playerId, out machine)) return machine;
            return Machine.Active();
        }

        // ブロックの原本は機体の種類ごと(通常・MP 用)に別の複製なので両方に付ける。MP で他人から見た機体の原本は撃たないので不要
        private static void PatchBlockPrefabs()
        {
            if (_blockPrefabsPatched) return;
            _blockPrefabsPatched = true;
            // 散弾砲の弾はダメージの部品を持たない(本体が力だけ与える)ので対象外
            var types = new[] { BlockType.Cannon, BlockType.Crossbow, BlockType.FuelCannon };
            foreach (var type in types)
            {
                BlockBehaviour prefab;
                if (PrefabMaster.GetBlock(type, PrefabMaster.PrefabType.Normal, out prefab)) PatchBlock(prefab);
                if (PrefabMaster.GetBlock(type, PrefabMaster.PrefabType.Network, out prefab)) PatchBlock(prefab);
            }
        }

        private static void PatchBlock(BlockBehaviour block)
        {
            var cannon = block as CanonBlock;
            if (cannon != null)
            {
                if (cannon.boltObjects != null)
                    foreach (var bolt in cannon.boltObjects)
                        if (bolt != null) PatchProjectile(bolt.gameObject);
                if (cannon.selectedBoltObject != null) PatchProjectile(cannon.selectedBoltObject.gameObject);
                return;
            }
            var crossbow = block as CrossBowBlock;
            if (crossbow != null)
            {
                PatchProjectile(crossbow.projectileNew);
                PatchRepeatFire(crossbow.shootingModule);
                return;
            }
            var fuelCannon = block as FlakCannonBlock;
            if (fuelCannon != null) PatchRepeatFire(fuelCannon.shootingModule);
        }

        private static void PatchRepeatFire(RepeatFire shooting)
        {
            if (shooting == null) return;
            PatchProjectile(shooting.projectile);
            PatchProjectile(shooting.projectilePrefab);
        }

        // 弾の見本(子を含む)に監視を付ける。爆発する砲弾の爆風の見本にも付ける(矢・火球は爆発しない)
        private static void PatchProjectile(GameObject projectile)
        {
            if (projectile == null) return;
            foreach (var cannonball in projectile.GetComponentsInChildren<CannonBallDamage>(true))
            {
                Ensure<VanillaCannonballWatch>(cannonball.gameObject);
                PatchBlast(cannonball.explosionPrefab);
                PatchBlast(cannonball.underwaterExplosionPrefab);
            }
            foreach (var arrow in projectile.GetComponentsInChildren<ProjectileScript>(true))
                Ensure<VanillaArrowWatch>(arrow.gameObject);
            foreach (var fireball in projectile.GetComponentsInChildren<FireBallDamage>(true))
                Ensure<VanillaFireballWatch>(fireball.gameObject);
            PatchPool(projectile);
        }

        private static void PatchBlast(GameObject blast)
        {
            if (blast != null && blast.GetComponent<CannonBallExplosion>() != null) Ensure<VanillaBlastWatch>(blast);
        }

        // MP ホストは弾をプールから出す。見本と同じ種類のプールの原本と、作成済みの弾に付ける
        private static void PatchPool(GameObject projectile)
        {
            var manager = ProjectileManager.Instance;
            var info = projectile.GetComponent<ProjectileInfo>();
            if (manager == null || info == null) return;
            if (manager != _patchedManager)
            {
                _patchedManager = manager;
                PatchedPoolTypes.Clear();
            }
            int type = (int)info.projectileType;
            if (!PatchedPoolTypes.Add(type)) return;
            if (manager.projectilePrefabs != null && type < manager.projectilePrefabs.Length)
                PatchProjectile(manager.projectilePrefabs[type]);
            var pool = manager.GetPool(type);
            if (pool == null) return;
            if (pool.Pool != null)
                foreach (var pooled in pool.Pool) if (pooled != null) PatchProjectile(pooled.gameObject);
            if (pool.Active != null)
                foreach (var active in pool.Active) if (active != null) PatchProjectile(active.gameObject);
        }

        private static void Ensure<T>(GameObject target) where T : Component
        {
            if (target.GetComponent<T>() == null) target.AddComponent<T>();
        }
    }
}
