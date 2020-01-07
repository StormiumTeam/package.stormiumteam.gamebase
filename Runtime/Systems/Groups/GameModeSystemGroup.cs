using Unity.Entities;
using Unity.NetCode;

namespace StormiumTeam.GameBase
{
	[DisableAutoCreation]
	public class BeginGameModeEntityCommandBufferSystem : EntityCommandBufferSystem
	{}
	
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class GameModeSystemGroup : ComponentSystemGroup
	{
		private GameModeManager m_GameModeManager;
		private BeginGameModeEntityCommandBufferSystem m_BeginBuffer;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_GameModeManager = World.GetOrCreateSystem<GameModeManager>();
			m_BeginBuffer = World.GetOrCreateSystem<BeginGameModeEntityCommandBufferSystem>();
		}

		protected override void OnUpdate()
		{
			m_GameModeManager.Update();
			m_BeginBuffer.Update();
			
			base.OnUpdate();
		}
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class ClientGameModeSystemGroup : ComponentSystemGroup
	{
	}
}