using Unity.Entities;

namespace StormiumTeam.GameBase.Utility.Modules
{
	public class ModuleRegister
	{
		public ComponentSystemBase System;

		public ModuleRegister(ComponentSystemBase system)
		{
			System = system;
		}
		
		public void GetModule<TModule>(out TModule module)
			where TModule : BaseSystemModule, new()
		{
			module = new TModule();
			module.Enable(System);
		}
	}
}