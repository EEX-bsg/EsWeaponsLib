using Modding;
using Modding.Modules;
using EWL.Modules;

namespace EWL
{
	public class Mod : ModEntryPoint
	{
		public override void OnLoad()
		{
			CustomModules.AddBlockModule<EwlWeaponModule, EwlWeaponBehaviour>("EwlWeapon", false);
			CustomModules.AddBlockModule<EwlBlockPhysicsModule, EwlBlockPhysicsBehaviour>("EwlBlockPhysics", false);
		}
	}
}
