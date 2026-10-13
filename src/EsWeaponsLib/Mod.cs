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
            CustomModules.AddBlockModule<EwlWeaponModule, EwlWeaponBehaviour>("EwlWeapon", false);
            CustomModules.AddBlockModule<EwlBlockPhysicsModule, EwlBlockPhysicsBehaviour>("EwlBlockPhysics", false);
            ProjectileSync.Initialize();
            // ゲーム終了まで購読したまま(Mod はアンロードされない)
            Events.OnBlockInit += ExplosiveTag.OnBlockInit;
            ModConsole.RegisterCommand("ewl_detonations", ExplosiveTag.LogDetonations, "Lists bombs exploded in this simulation and who detonated them (EWL attacker tags).");
        }
    }
}
