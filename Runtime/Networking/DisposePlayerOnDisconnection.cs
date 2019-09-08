using Revolution.NetCode;
using StormiumTeam.GameBase.EcsComponents;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class DisposePlayerOnDisconnection : JobComponentSystem
	{
		private struct Job : IJobForEachWithEntity<NetworkOwner, GamePlayer>
		{
			public            EntityCommandBuffer.Concurrent                     CommandBuffer;
			[ReadOnly] public ComponentDataFromEntity<NetworkStreamConnection>   ConnectionFromEntity;
			[ReadOnly] public ComponentDataFromEntity<NetworkStreamDisconnected> DisconnectedTagFromEntity;

			public void Execute(Entity entity, int jobIndex, ref NetworkOwner netOwner, ref GamePlayer gamePlayer)
			{
				if (!ConnectionFromEntity.Exists(netOwner.Value) || DisconnectedTagFromEntity.Exists(netOwner.Value))
					CommandBuffer.DestroyEntity(jobIndex, entity);
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
			return new Job
			{
				CommandBuffer             = m_Barrier.CreateCommandBuffer().ToConcurrent(),
				ConnectionFromEntity      = GetComponentDataFromEntity<NetworkStreamConnection>(),
				DisconnectedTagFromEntity = GetComponentDataFromEntity<NetworkStreamDisconnected>()
			}.Schedule(this, inputDeps);
		}
	}
}