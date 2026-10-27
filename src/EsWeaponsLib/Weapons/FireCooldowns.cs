using System.Collections.Generic;
using EWL.Besiege;
using UnityEngine;

namespace EWL.Weapons
{
    // 次の発射可能時刻を駆動の外に持ち、レベルロジックのリスポーンで武装が作り直されても発射間隔の残りを引き継ぐ。
    // 発射の権威側は Burst で記録する。MP クライアント(ホストのシミュを表示中)は、届いた発射から数えた目安を記録する。
    // キーはマシン(リスポーンで変わらない)・ビルドブロックの Guid・武装の生成順。
    internal static class FireCooldowns
    {
        private struct Entry
        {
            public Machine Machine;
            public float NextFireTime;
            public bool Estimated; // 権威の無いピアの目安。権威側の値としては使わない
        }

        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
        private static readonly List<string> Expired = new List<string>();
        private static bool _hasEstimates;

        // 権威側の記録はリスポーンのときだけ引き継ぐ。通常のシミュ開始では捨て、次のリスポーンで古いシミュの値が出てこないようにする。
        // MP クライアントでは本体がリスポーンの印を立てないので、目安はレベルシミュが途切れていない間だけ引き継ぐ(途切れたら ForgetEstimates が捨てる)
        internal static float Restore(BlockBehaviour block, int networkIndex)
        {
            var key = KeyFor(block, networkIndex);
            Entry entry;
            if (key == null || !Entries.TryGetValue(key, out entry)) return 0f;
            bool usable = entry.Estimated ? Simulation.ShowsRemoteProjectiles : Simulation.IsRespawning(block);
            if (!usable)
            {
                Entries.Remove(key);
                return 0f;
            }
            return entry.Machine == block.ParentMachine ? entry.NextFireTime : 0f;
        }

        internal static void Record(BlockBehaviour block, int networkIndex, float nextFireTime)
        {
            Store(block, networkIndex, nextFireTime, false);
        }

        internal static void RecordEstimate(BlockBehaviour block, int networkIndex, float nextFireTime)
        {
            if (Store(block, networkIndex, nextFireTime, true)) _hasEstimates = true;
        }

        /// <summary>レベルシミュが止まっている間に呼ぶ(毎フレームでよい)。目安の記録を捨てる。</summary>
        internal static void ForgetEstimates()
        {
            if (!_hasEstimates) return;
            _hasEstimates = false;
            foreach (var pair in Entries)
                if (pair.Value.Estimated) Expired.Add(pair.Key);
            foreach (var key in Expired) Entries.Remove(key);
            Expired.Clear();
        }

        private static bool Store(BlockBehaviour block, int networkIndex, float nextFireTime, bool estimated)
        {
            var key = KeyFor(block, networkIndex);
            if (key == null) return false;
            Prune();
            Entries[key] = new Entry { Machine = block.ParentMachine, NextFireTime = nextFireTime, Estimated = estimated };
            return true;
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
