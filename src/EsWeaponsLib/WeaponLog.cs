using EWL.Besiege;
using EWL.Weapons;
using UnityEngine;

namespace EWL
{
    // コンソールコマンド ewl_weapons: このピアにある武装の状態(発射間隔の残り・弾数・権威)を出す(MP の表示値の確認用)。
    internal static class WeaponLog
    {
        internal static void Command(string[] args)
        {
            var drivers = Object.FindObjectsOfType<WeaponDriver>();
            Modding.ModConsole.Log("[EWL] weapons: " + drivers.Length + " levelSimulating=" + Simulation.IsLevelSimulating
                + " showsRemote=" + Simulation.ShowsRemoteProjectiles + " time=" + Time.time.ToString("F2"));
            foreach (var driver in drivers)
            {
                var block = driver.Block;
                Modding.ModConsole.Log("[EWL] weapon player=" + Simulation.PlayerIdOf(block)
                    + " block=" + (block != null ? block.name + " #" + block.BuildIndex : "none") + "/" + driver.NetworkIndex
                    + " authority=" + Simulation.HasAuthority(block)
                    + " cooldown=" + driver.FireCooldownRemaining.ToString("F2")
                    + " ammo=" + driver.Ammo + " reserve=" + driver.ReserveAmmo + " reloading=" + driver.IsReloading);
            }
        }
    }
}
