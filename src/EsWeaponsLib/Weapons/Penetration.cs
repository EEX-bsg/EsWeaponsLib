using UnityEngine;

namespace EWL.Weapons
{
    // 貫通: 命中点から進行方向へ、通った装甲の厚みの合計が上限に達する所(上限までに装甲を抜けたら抜けた所)を求める。
    // 線分上の全コライダーの入口を前向き、出口を逆向きのレイ1本ずつで取り、コライダーごとの区間を近い順にたどる。
    // 重なった区間(1ブロックの複数コライダー・ブロック同士の重なり)は和集合として数えるので二重に数えない。
    // firstBlockOnly なら命中したブロック(剛体。剛体が無ければコライダー)の区間だけを数え、そのブロックを抜けた所で止まる。
    internal static class Penetration
    {
        private const float StartBack = 0.05f;     // 命中点から最低これだけ手前から探す
        private const float GapTolerance = 0.05f;  // これより広い隙間があれば、装甲がそこで途切れたとみなす
        private const float SearchMargin = 0.5f;   // 上限より先まで探す長さ(装甲の間の小さな隙間の分)
        private const int MaxHits = 64;

        private static readonly RaycastHit[] RaycastHits = new RaycastHit[MaxHits];
        private static readonly Collider[] SpanColliders = new Collider[MaxHits];
        private static readonly float[] SpanStarts = new float[MaxHits];
        private static readonly float[] SpanEnds = new float[MaxHits];
        private static int _spanCount;
        private static readonly Collider[] PassedColliders = new Collider[MaxHits];
        private static readonly Vector3[] PassedPoints = new Vector3[MaxHits];
        private static int _passedCount;

        /// <summary>直前の <see cref="Resolve"/> で通ったブロックの数(止まったブロックを含む。ブロックごとに1つ、近い順)。</summary>
        internal static int PassedCount { get { return _passedCount; } }

        /// <summary>直前の <see cref="Resolve"/> で通った i 番目のブロックのコライダー。</summary>
        internal static Collider Passed(int index) { return PassedColliders[index]; }

        /// <summary>直前の <see cref="Resolve"/> で i 番目のブロックに入った点。</summary>
        internal static Vector3 PassedPoint(int index) { return PassedPoints[index]; }

        /// <summary>
        /// 止まる位置と、そこにある(抜けた場合は最後に抜けた)コライダーを求める。厚みは命中したコライダーの入口から測る。
        /// exited: 上限に達する前に装甲を抜けた(stopPoint は出口)。used: 使った貫通量 [m]。
        /// back: 命中点からどれだけ手前から探すか [m]。速い弾は接触が装甲の中で報告されることがあるので、1物理フレームに進む距離を渡す。
        /// clearance: 抜けた弾が出口の先に入るのに要る長さ [m]。これより狭い隙間の先にある装甲は、抜けずに続けて数える
        /// (firstBlockOnly では、次のブロックがこれより近ければ抜けずに裏面で止まる)。
        /// false なら貫通の判定ができなかった(装甲が見つからない等)。通常の命中として扱う。
        /// </summary>
        internal static bool Resolve(Vector3 point, Vector3 direction, float back, float depth, float clearance, Collider hit, bool firstBlockOnly,
            Rigidbody self, int layerMask, out Vector3 stopPoint, out Collider stopCollider, out bool exited, out float used)
        {
            stopPoint = point;
            stopCollider = null;
            exited = false;
            used = 0f;
            _passedCount = 0;
            back = Mathf.Max(back, StartBack);
            var origin = point - direction * back;
            float length = back + depth + Mathf.Max(SearchMargin, clearance + GapTolerance);
            float gap = Mathf.Max(GapTolerance, clearance);
            _spanCount = 0;

            int count = Physics.RaycastNonAlloc(origin, direction, RaycastHits, length, layerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (Counts(RaycastHits[i].collider, self)) AddEntry(RaycastHits[i].collider, RaycastHits[i].distance);
            count = Physics.RaycastNonAlloc(origin + direction * length, -direction, RaycastHits, length, layerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (Counts(RaycastHits[i].collider, self)) AddExit(RaycastHits[i].collider, length - RaycastHits[i].distance);
            SortByStart(length);

            // 命中したコライダーの入口から数える(手前で探した分の、命中より前の物は数えない)。見つからなければ接触点から
            float cursor = back;
            for (int i = 0; i < _spanCount; i++)
                if (SpanColliders[i] == hit) { cursor = SpanStarts[i]; break; }
            float remaining = depth;
            Object hitBlock = BlockOf(hit); // 区間の先頭ではなく実際に当たった物(隣のブロックのコライダーが手前にはみ出していることがある)
            bool started = false;
            for (int i = 0; i < _spanCount; i++)
            {
                if (firstBlockOnly && BlockOf(SpanColliders[i]) != hitBlock) continue;
                float end = SpanEnds[i];
                if (end <= cursor) continue; // 命中より手前か、先の区間に含まれる
                float start = Mathf.Max(SpanStarts[i], cursor);
                if (start - cursor > gap) break; // 装甲が途切れた(弾が入れる隙間がある)
                started = true;
                AddPassed(SpanColliders[i], origin + direction * start);
                float thickness = end - start;
                if (thickness >= remaining)
                {
                    stopPoint = origin + direction * (start + remaining);
                    stopCollider = SpanColliders[i];
                    used = depth;
                    return true;
                }
                remaining -= thickness;
                cursor = end;
                stopCollider = SpanColliders[i];
            }
            if (!started) return false;
            stopPoint = origin + direction * cursor;
            used = depth - remaining;
            exited = true;
            if (firstBlockOnly) // 裏に別のブロックが近ければ、抜けた弾が入らないので裏面で止まる
                for (int i = 0; i < _spanCount; i++)
                    if (SpanEnds[i] > cursor && SpanStarts[i] - cursor <= gap) { exited = false; break; }
            return true;
        }

        private static void AddPassed(Collider collider, Vector3 entry)
        {
            var block = BlockOf(collider);
            for (int i = 0; i < _passedCount; i++)
                if (BlockOf(PassedColliders[i]) == block) return;
            if (_passedCount == MaxHits) return;
            PassedColliders[_passedCount] = collider;
            PassedPoints[_passedCount++] = entry;
        }

        private static Object BlockOf(Collider collider)
        {
            var rb = collider.attachedRigidbody;
            return rb != null ? (Object)rb : collider;
        }

        private static bool Counts(Collider collider, Rigidbody self)
        {
            if (collider == null) return false;
            var rb = collider.attachedRigidbody;
            return rb == null || (rb != self && rb.GetComponent<Projectile>() == null); // 弾自身・他の弾は装甲に数えない
        }

        private static int IndexOf(Collider collider)
        {
            for (int i = 0; i < _spanCount; i++)
                if (SpanColliders[i] == collider) return i;
            if (_spanCount == MaxHits) return -1;
            SpanColliders[_spanCount] = collider;
            SpanStarts[_spanCount] = float.NaN;
            SpanEnds[_spanCount] = float.NaN;
            return _spanCount++;
        }

        private static void AddEntry(Collider collider, float distance)
        {
            int i = IndexOf(collider);
            if (i < 0) return;
            if (float.IsNaN(SpanStarts[i]) || distance < SpanStarts[i]) SpanStarts[i] = distance;
        }

        private static void AddExit(Collider collider, float distance)
        {
            int i = IndexOf(collider);
            if (i < 0) return;
            if (float.IsNaN(SpanEnds[i]) || distance > SpanEnds[i]) SpanEnds[i] = distance;
        }

        // 入口が無い = 探し始めより手前から続く、出口が無い = 線分の終わりより先まで続く。その上で入口の近い順に並べる。
        private static void SortByStart(float length)
        {
            for (int i = 0; i < _spanCount; i++)
            {
                if (float.IsNaN(SpanStarts[i]) || SpanStarts[i] < 0f) SpanStarts[i] = 0f;
                if (float.IsNaN(SpanEnds[i])) SpanEnds[i] = length;
            }
            for (int i = 1; i < _spanCount; i++)
            {
                var collider = SpanColliders[i];
                float start = SpanStarts[i], end = SpanEnds[i];
                int j = i - 1;
                for (; j >= 0 && SpanStarts[j] > start; j--)
                {
                    SpanColliders[j + 1] = SpanColliders[j];
                    SpanStarts[j + 1] = SpanStarts[j];
                    SpanEnds[j + 1] = SpanEnds[j];
                }
                SpanColliders[j + 1] = collider;
                SpanStarts[j + 1] = start;
                SpanEnds[j + 1] = end;
            }
        }
    }
}
