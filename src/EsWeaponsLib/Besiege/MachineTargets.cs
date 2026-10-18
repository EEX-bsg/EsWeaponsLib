using System.Collections.Generic;
using UnityEngine;

namespace EWL.Besiege
{
    // 誘導の目標 = 敵マシンのコアブロック(シミュ用)の Transform と、テスト用のダミー標的(TestTargets)。
    // 敵 = 撃ったブロックのマシン以外で、観戦中でなく、このピアから見てグローバルシミュ中のマシン。撃ったブロックにチームがあれば同じチームは除く(チーム無しは全員が敵)。
    // SP とローカルシミュには他人がいないのでダミーだけ。どのピアでも同じ判定になる(クライアントの表示とホストの確定を一致させる)。
    internal static class MachineTargets
    {
        /// <summary>テスト用のダミー標的。全員の敵として扱う(このピアだけ。同期しない)。置き場は TestTargets。</summary>
        internal static readonly List<Transform> Dummies = new List<Transform>();

        /// <summary>照準 origin・direction から maxAngle 度以内の目標のうち、照準とのなす角が最も小さいもの。無ければ null。毎フレーム呼んでよい(割り当てなし)。</summary>
        internal static Transform FindNearestToAim(BlockBehaviour shooter, Vector3 origin, Vector3 direction, float maxAngle)
        {
            if (!CanShoot(shooter)) return null;
            Transform best = null;
            float bestAngle = maxAngle;
            if (HasEnemyPlayers)
            {
                var players = Playerlist.Players;
                for (int i = 0; i < players.Count; i++)
                {
                    var core = EnemyCoreOf(shooter, players[i]);
                    if (core != null) Consider(core.transform, origin, direction, ref best, ref bestAngle);
                }
            }
            for (int i = 0; i < Dummies.Count; i++)
                if (Dummies[i] != null) Consider(Dummies[i], origin, direction, ref best, ref bestAngle);
            return best;
        }

        /// <summary>target が shooter から見て今も有効な目標(敵のコアかダミー)か。</summary>
        internal static bool IsValidTarget(BlockBehaviour shooter, Transform target)
        {
            if (target == null || !CanShoot(shooter)) return false;
            if (Dummies.Contains(target)) return true;
            if (!HasEnemyPlayers) return false;
            var players = Playerlist.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var core = EnemyCoreOf(shooter, players[i]);
                if (core != null && core.transform == target) return true;
            }
            return false;
        }

        /// <summary>目標のプレイヤー番号(MP の送信用)。敵のコアでなければ(ダミーを含む) false。</summary>
        internal static bool TryGetPlayerId(Transform target, out ushort playerId)
        {
            var block = target != null ? target.GetComponent<BlockBehaviour>() : null;
            playerId = block != null ? Simulation.PlayerIdOf(block) : (ushort)0;
            return block != null && block.Prefab.Type == BlockType.StartingBlock;
        }

        /// <summary>プレイヤー番号のマシンのコア(敵かどうかは見ない)。MP の受信で目標を引くのに使う。</summary>
        internal static Transform CoreOfPlayer(ushort playerId)
        {
            var players = Playerlist.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player.machine == null || player.machine.PlayerID != playerId) continue;
                var core = SimulationCore(player.machine);
                return core != null ? core.transform : null;
            }
            return null;
        }

        private static void Consider(Transform candidate, Vector3 origin, Vector3 direction, ref Transform best, ref float bestAngle)
        {
            float angle = Vector3.Angle(direction, candidate.position - origin);
            if (angle > bestAngle) return;
            best = candidate;
            bestAngle = angle;
        }

        private static bool CanShoot(BlockBehaviour shooter)
        {
            return shooter != null && shooter.isSimulating;
        }

        // 他のプレイヤーのマシンが同じシミュにいるのは MP のグローバルシミュだけ
        private static bool HasEnemyPlayers { get { return StatMaster.isMP && !StatMaster.isLocalSim; } }

        private static BlockBehaviour EnemyCoreOf(BlockBehaviour shooter, PlayerData player)
        {
            var machine = player.machine;
            if (player.isSpectator || machine == null || machine == shooter.ParentMachine) return null;
            if (!machine.isSimulating || machine.RemoteLocal) return null; // ローカルシミュ中の他人はグローバルシミュにいない
            if (shooter.Team != MPTeam.None && player.team == shooter.Team) return null;
            var core = SimulationCore(machine);
            return core != null && core.isSimulating && core.gameObject.activeInHierarchy ? core : null;
        }

        // SimulationBlocks は呼ぶたびに複製を作るので、マシンごとにコアを覚える。見つからない間は1秒ごとに探し直す
        private const float CoreRetryInterval = 1f;
        private static readonly Dictionary<Machine, CoreEntry> Cores = new Dictionary<Machine, CoreEntry>();
        private static readonly List<Machine> Removing = new List<Machine>();

        private struct CoreEntry
        {
            internal BlockBehaviour Core;
            internal float RetryTime;
        }

        private static BlockBehaviour SimulationCore(Machine machine)
        {
            CoreEntry entry;
            if (Cores.TryGetValue(machine, out entry))
            {
                // リスポーン・シミュ終了で前のシミュ用ブロックは破棄される
                if (entry.Core != null && entry.Core.isSimulating) return entry.Core;
                if (Time.time < entry.RetryTime) return null;
            }
            entry.Core = null;
            entry.RetryTime = Time.time + CoreRetryInterval;
            foreach (var block in machine.SimulationBlocks)
            {
                if (block != null && block.Prefab.Type == BlockType.StartingBlock) { entry.Core = block; break; }
            }
            ForgetDestroyedMachines();
            Cores[machine] = entry;
            return entry.Core;
        }

        private static void ForgetDestroyedMachines()
        {
            Removing.Clear();
            foreach (var machine in Cores.Keys)
                if (machine == null) Removing.Add(machine);
            foreach (var machine in Removing) Cores.Remove(machine);
        }
    }
}
