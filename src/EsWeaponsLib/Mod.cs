using Modding;
using Modding.Modules;
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
        }
    }
}
