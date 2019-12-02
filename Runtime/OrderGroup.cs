using Unity.NetCode;
using Unity.Entities;

namespace StormiumTeam.GameBase
{
	public static class OrderGroup
	{
		[UpdateInGroup(typeof(ClientAndServerInitializationSystemGroup))]
		public class PreFrame : ComponentSystemGroup
		{
			[UpdateInGroup(typeof(PreFrame))]
			public class Rules : ComponentSystemGroup
			{
					
			}
		}
		
		[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
		[UpdateAfter(typeof(NetworkReceiveSnapshotSystemGroup))]
		[UpdateBefore(typeof(SnapshotSendSystem))]
		public class Simulation : ComponentSystemGroup
		{
			[UpdateInGroup(typeof(Simulation))]
			public class Initialization : ComponentSystemGroup
			{
			}

			[UpdateInGroup(typeof(Simulation))]
			[UpdateAfter(typeof(Initialization))]
			public class UpdateEntities : ComponentSystemGroup
			{
				[UpdateInGroup(typeof(UpdateEntities))]
				public class Interaction : ComponentSystemGroup
				{
					
				}

				[UpdateInGroup(typeof(UpdateEntities))]
				[UpdateAfter(typeof(Interaction))]
				public class Rules : ComponentSystemGroup
				{
					
				}
				
				[UpdateInGroup(typeof(UpdateEntities))]
				[UpdateAfter(typeof(Rules))]
				public class GameMode : ComponentSystemGroup
				{
					
				}
			}

			[UpdateInGroup(typeof(Simulation))]
			[UpdateAfter(typeof(UpdateEntities))]
			public class DeleteEntities : ComponentSystemGroup
			{
				[UpdateInGroup(typeof(DeleteEntities))]
				public class CommandBufferSystem : EntityCommandBufferSystem
				{
				}
			}
			
			public class BeforeSpawnEntitiesCommandBuffer : EntityCommandBufferSystem
			{}

			[UpdateInGroup(typeof(Simulation))]
			[UpdateAfter(typeof(DeleteEntities))]
			public class SpawnEntities : ComponentSystemGroup
			{				
				[UpdateInGroup(typeof(SpawnEntities))]
				public class SpawnEvent : ComponentSystemGroup
				{

				}
				
				[UpdateInGroup(typeof(SpawnEntities))]
				[UpdateAfter(typeof(SpawnEvent))]
				public class CommandBufferSystem : EntityCommandBufferSystem
				{}
			}

			[UpdateInGroup(typeof(Simulation))]
			[UpdateAfter(typeof(SpawnEntities))]
			public class ConfigureSpawnedEntities : ComponentSystemGroup
			{

			}

			[UpdateInGroup(typeof(Simulation))]
			[UpdateAfter(typeof(ConfigureSpawnedEntities))]
			public class ProcessEvents : ComponentSystemGroup
			{
				
			}
		}
	}
}