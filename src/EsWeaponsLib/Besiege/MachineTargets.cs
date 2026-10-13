using UnityEngine;

namespace EWL.Besiege
{
    // 誘導の目標探し(仮実装)。呼ぶのはシミュ権威側の発射時だけ。
    internal static class MachineTargets
    {
        /// <summary>
        /// origin から direction の前方 maxAngle 度以内にある敵マシンのコアブロックのうち、最も近いもの。無ければ null。
        /// 敵 = 自分以外で、このピアが物理を動かしているシミュ中のマシン(SP とローカルシミュでは他人がいないので常に null)。
        /// 撃ったブロックにチームがあれば同じチームは除く(チーム無しは全員が敵)。
        /// </summary>
        internal static Transform FindEnemyCore(BlockBehaviour shooter, Vector3 origin, Vector3 direction, float maxAngle)
        {
            if (!StatMaster.isMP || shooter == null) return null;
            Transform best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (var player in Playerlist.Players)
            {
                var machine = player.machine;
                if (player.isSpectator || machine == null || machine == shooter.ParentMachine) continue;
                if (!machine.isSimulating || !machine.SimPhysics) continue;
                if (shooter.Team != MPTeam.None && player.team == shooter.Team) continue;
                foreach (var block in machine.SimulationBlocks) // 発射時だけなので複製の割り当ては許容
                {
                    if (block == null || block.Prefab.Type != BlockType.StartingBlock || !block.gameObject.activeInHierarchy) continue;
                    var offset = block.transform.position - origin;
                    float distance = offset.magnitude;
                    if (distance >= bestDistance || Vector3.Angle(direction, offset) > maxAngle) continue;
                    best = block.transform;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
