using StormiumTeam.GameBase.BaseSystems.Interfaces;
using StormiumTeam.GameBase.Utility.Modules;
using Unity.Entities;

namespace StormiumTeam.GameBase.BaseSystems
{
	public abstract class AbsGameBaseSystem : SystemBase, IGameBaseSystem
	{
		private ComponentSystemGroup m_ClientPresentationGroup;

		private ModuleRegister m_ModuleRegister;

		protected override void OnCreate()
		{
			m_ModuleRegister = new ModuleRegister(this);
		}

		public void GetModule<TModule>(out TModule module)
			where TModule : BaseSystemModule, new()
		{
			m_ModuleRegister.GetModule(out module);
		}
	}
}