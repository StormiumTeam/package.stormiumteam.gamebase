using Unity.Entities;
using Unity.NetCode;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class GameModeSystemGroup : ComponentSystemGroup
	{
		private GameModeManager m_GameModeManager;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_GameModeManager = World.GetOrCreateSystem<GameModeManager>();
		}

		protected override void OnUpdate()
		{
			m_GameModeManager.Update();
			
			base.OnUpdate();
		}
	}
	
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class ClientGameModeSystemGroup : ComponentSystemGroup
	{}
}