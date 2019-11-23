using Revolution.NetCode;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class ClientLoadSystem : JobComponentSystem
	{
		private EndSimulationEntityCommandBufferSystem m_Barrier;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var ecb     = m_Barrier.CreateCommandBuffer().ToConcurrent();
			var version = GameStatic.Version;
			inputDeps = Entities
			            .WithNone<NetworkStreamInGame>()
			            .ForEach((Entity connection, int nativeThreadIndex, ref NetworkIdComponent id) =>
			            {
				            ecb.AddComponent(nativeThreadIndex, connection, default(NetworkStreamInGame));

				            var reqEnt = ecb.CreateEntity(nativeThreadIndex);
				            ecb.AddComponent(nativeThreadIndex, reqEnt, new ClientLoadedRpc {GameVersion                     = version});
				            ecb.AddComponent(nativeThreadIndex, reqEnt, new SendRpcCommandRequestComponent {TargetConnection = connection});
			            }).Schedule(inputDeps);

			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}