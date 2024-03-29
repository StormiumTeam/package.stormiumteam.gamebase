using Unity.Entities;
using Unity.Transforms;

namespace StormiumTeam.GameBase
{
	public static class OrderGroup
	{
		[UpdateInGroup(typeof(InitializationSystemGroup))]
		public class PreFrame : ComponentSystemGroup
		{
			[UpdateInGroup(typeof(PreFrame))]
			public class Rules : ComponentSystemGroup
			{
			}
		}

		[UpdateInGroup(typeof(SimulationSystemGroup))]
		/*[UpdateAfter(typeof(NetworkReceiveSnapshotSystemGroup))]
		[UpdateAfter(typeof(GhostSimulationSystemGroup))]
		[UpdateAfter(typeof(GhostPredictionSystemGroup))]
		[UpdateBefore(typeof(SnapshotSendSystem))]*/
		public class Simulation : ComponentSystemGroup
		{
			[UpdateInGroup(typeof(Simulation))]
			public class Initialization : ComponentSystemGroup
			{
			}

			[UpdateInGroup(typeof(Simulation))]
			[UpdateAfter(typeof(Initialization))]
			public class DeleteEntities : ComponentSystemGroup
			{
				[UpdateInGroup(typeof(DeleteEntities))]
				public class CommandBufferSystem : EntityCommandBufferSystem
				{
				}
			}

			public class BeforeSpawnEntitiesCommandBuffer : EntityCommandBufferSystem
			{
			}

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
				{
				}
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

			[UpdateInGroup(typeof(Simulation))]
			[UpdateAfter(typeof(ProcessEvents))]
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
		}

		public class Presentation
		{
			[UpdateInGroup(typeof(PresentationSystemGroup))]
			/*[UpdateAfter(typeof(RenderInterpolationSystem))]*/
			[UpdateAfter(typeof(TransformSystemGroup))]
			public class AfterSimulation : ComponentSystemGroup
			{
			}

			[UpdateInGroup(typeof(PresentationSystemGroup))]
			[UpdateAfter(typeof(AfterSimulation))]
			public class CharacterAnimation : ComponentSystemGroup
			{
			}

			[UpdateInGroup(typeof(PresentationSystemGroup))]
			[UpdateAfter(typeof(CharacterAnimation))]
			public class UpdateCamera : ComponentSystemGroup
			{
			}

			[UpdateInGroup(typeof(PresentationSystemGroup))]
			[UpdateAfter(typeof(UpdateCamera))]
			public class CopyToGameObject : ComponentSystemGroup
			{
			}

			[UpdateInGroup(typeof(PresentationSystemGroup))]
			[UpdateAfter(typeof(CopyToGameObject))]
			public class InterfaceRendering : ComponentSystemGroup
			{
			}
		}
	}
}