using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

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

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> OutgoingDataFromEntity;

			public void Execute(Entity connection, int jobIndex, ref NetworkIdComponent id)
			{
				CommandBuffer.AddComponent(jobIndex, connection, default(NetworkStreamInGame));

				RpcQueue.Schedule(OutgoingDataFromEntity[connection], new ClientLoadedRpc());
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
				CommandBuffer          = m_Barrier.CreateCommandBuffer().ToConcurrent(),
				OutgoingDataFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>(),
				RpcQueue               = World.GetExistingSystem<RpcQueueSystem<ClientLoadedRpc>>().GetRpcQueue()
			}.Schedule(this);
			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}