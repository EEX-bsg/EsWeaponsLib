using Modding;
using Modding.Modules;
using EWL.Besiege;
using EWL.Modules;
using EWL.Net;

namespace EWL
{
    public class Mod : ModEntryPoint
    {
        public override void OnLoad()
        {
            CustomModules.AddBlockModule<EwlWeaponModule, EwlWeaponBehaviour>("EwlWeapon", true);
            CustomModules.AddBlockModule<EwlBlockPhysicsModule, EwlBlockPhysicsBehaviour>("EwlBlockPhysics", true);
            ProjectileSync.Initialize();
            // ゲーム終了まで購読したまま(Mod はアンロードされない)
            Events.OnBlockInit += ExplosiveTag.OnBlockInit;
            Events.OnBlockInit += VanillaProjectiles.OnBlockInit;
            ModConsole.RegisterCommand("ewl_detonations", ExplosiveTag.LogDetonations, "Lists bombs exploded in this simulation and who detonated them (EWL attacker tags).");
            ModConsole.RegisterCommand("ewl_dummy", TestTargets.Command, "Places test targets that EWL guidance treats as enemies on this peer only (not synced). Usage: ewl_dummy add [distance] | clear.");
            ModConsole.RegisterCommand("ewl_hits", HitLog.Command, "Logs every projectile hit reported by Weapon.AnyHit (EWL and vanilla) to output_log.txt. Usage: ewl_hits [on|off].");
            ModConsole.RegisterCommand("ewl_weapons", WeaponLog.Command, "Lists EWL weapons on this peer with fire cooldown remaining, ammo and authority.");
        }
    }
}
