using Revolution.NetCode;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class ClientLoadSystem : JobComponentSystem
	{
		[ExcludeComponent(typeof(NetworkStreamInGame))]
		private struct Job : IJobForEachWithEntity<NetworkIdComponent>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			public RpcQueue<ClientLoadedRpc>      RpcQueue;

			public int GameVersion;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> OutgoingDataFromEntity;

			public void Execute(Entity connection, int jobIndex, ref NetworkIdComponent id)
			{
				CommandBuffer.AddComponent(jobIndex, connection, default(NetworkStreamInGame));

				//RpcQueue.Schedule(OutgoingDataFromEntity[connection], new ClientLoadedRpc {GameVersion = GameVersion});
				var reqEnt = CommandBuffer.CreateEntity(jobIndex);
				CommandBuffer.AddComponent(jobIndex, reqEnt, new ClientLoadedRpc {GameVersion = GameVersion});
				CommandBuffer.AddComponent(jobIndex, reqEnt, new SendRpcCommandRequestComponent {TargetConnection = connection});
			}
		}

		private EndSimulationEntityCommandBufferSystem m_Barrier;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new Job
			{
				GameVersion = GameStatic.Version,
				
				CommandBuffer          = m_Barrier.CreateCommandBuffer().ToConcurrent(),
				OutgoingDataFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>(),
				RpcQueue               = World.GetExistingSystem<RpcCommandRequest<ClientLoadedRpc>>().Queue
			}.Schedule(this, inputDeps);
			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}