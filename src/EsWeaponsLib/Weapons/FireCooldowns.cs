using System.Collections.Generic;
using EWL.Besiege;
using UnityEngine;

namespace EWL.Weapons
{
    // 次の発射可能時刻を駆動の外に持ち、レベルロジックのリスポーンで武装が作り直されても発射間隔の残りを引き継ぐ。
    // 書くのは発射の権威側(Burst)だけ。キーはマシン(リスポーンで変わらない)・ビルドブロックの Guid・武装の生成順。
    internal static class FireCooldowns
    {
        private struct Entry
        {
            public Machine Machine;
            public float NextFireTime;
        }

        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
        private static readonly List<string> Expired = new List<string>();

        // リスポーンのときだけ引き継ぐ。通常のシミュ開始では記録を捨て、次のリスポーンで古いシミュの値が出てこないようにする
        internal static float Restore(BlockBehaviour block, int networkIndex)
        {
            var key = KeyFor(block, networkIndex);
            Entry entry;
            if (key == null || !Entries.TryGetValue(key, out entry)) return 0f;
            if (!Simulation.IsRespawning(block))
            {
                Entries.Remove(key);
                return 0f;
            }
            return entry.Machine == block.ParentMachine ? entry.NextFireTime : 0f;
        }

        internal static void Record(BlockBehaviour block, int networkIndex, float nextFireTime)
        {
            var key = KeyFor(block, networkIndex);
            if (key == null) return;
            Prune();
            Entries[key] = new Entry { Machine = block.ParentMachine, NextFireTime = nextFireTime };
        }

        // マシン参照を比べるので、同じ PlayerID の別人・別マシンには引き継がれない。破棄済みのマシンと過ぎた時刻は捨てる
        private static void Prune()
        {
            float now = Time.time;
            foreach (var pair in Entries)
                if (pair.Value.Machine == null || pair.Value.NextFireTime <= now) Expired.Add(pair.Key);
            foreach (var key in Expired) Entries.Remove(key);
            Expired.Clear();
        }

        // シミュ用ブロックは毎回複製され Guid を持たないので、ビルドブロックの Guid を使う
        private static string KeyFor(BlockBehaviour block, int networkIndex)
        {
            if (block == null || block.ParentMachine == null || block.BuildingBlock == null
                || block.BuildingBlock.Guid == System.Guid.Empty) return null;
            return block.ParentMachine.GetInstanceID() + "/" + block.BuildingBlock.Guid + "/" + networkIndex;
        }
    }
}
