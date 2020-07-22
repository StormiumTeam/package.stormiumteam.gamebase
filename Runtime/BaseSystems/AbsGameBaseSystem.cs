using GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.BaseSystems.Interfaces;
using StormiumTeam.GameBase.Utility.Modules;
using Unity.Entities;

namespace StormiumTeam.GameBase.BaseSystems
{
	public abstract class AbsGameBaseSystem : SystemBase, IGameBaseSystem
	{
		private ComponentSystemGroup m_ClientPresentationGroup;
		private EntityQuery          m_LocalPlayerGroup;

		private ModuleRegister m_ModuleRegister;

		private EntityQuery m_PlayerGroup;

		EntityQuery IGameBaseSystem.GetPlayerGroup()
		{
			return m_PlayerGroup;
		}

		EntityQuery IGameBaseSystem.GetLocalPlayerGroup()
		{
			return m_LocalPlayerGroup;
		}

		protected override void OnCreate()
		{
			m_LocalPlayerGroup = GetEntityQuery
			(
				typeof(PlayerDescription), typeof(PlayerIsLocal)
			);

			m_PlayerGroup = GetEntityQuery
			(
				typeof(PlayerDescription)
			);

			m_ModuleRegister = new ModuleRegister(this);
		}

		public void GetModule<TModule>(out TModule module)
			where TModule : BaseSystemModule, new()
		{
			m_ModuleRegister.GetModule(out module);
		}
	}
}