using EWL.Api;
using UnityEngine;

namespace EWL
{
    // コンソールコマンド ewl_hits: Weapon.AnyHit をログ(output_log.txt)に出す(命中通知の確認用)。ゲーム終了まで、または off まで購読する。
    internal static class HitLog
    {
        private static bool _enabled;

        internal static void Command(string[] args)
        {
            bool enable = args.Length == 0 ? !_enabled : args[0] != "off";
            if (enable != _enabled)
            {
                if (enable) Weapon.AnyHit += Log;
                else Weapon.AnyHit -= Log;
                _enabled = enable;
            }
            Modding.ModConsole.Log("[EWL] hit log " + (_enabled ? "on" : "off"));
        }

        private static void Log(ProjectileHit hit)
        {
            Debug.Log("[EWL] hit " + hit.Source + " " + hit.Kind
                + " shooter=" + (hit.Shooter != null ? hit.Shooter.PlayerId.ToString() : "unknown")
                + " block=" + (hit.Block != null ? hit.Block.name + " #" + hit.Block.BuildIndex : "none")
                + " collider=" + (hit.Collider != null ? hit.Collider.name : "none")
                + " point=" + hit.Point);
        }
    }
}
