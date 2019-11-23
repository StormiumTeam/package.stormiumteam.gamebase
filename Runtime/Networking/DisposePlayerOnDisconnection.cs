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
		private EndSimulationEntityCommandBufferSystem m_Barrier;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var ecb                     = m_Barrier.CreateCommandBuffer().ToConcurrent();
			var connectionFromEntity    = GetComponentDataFromEntity<NetworkStreamConnection>(true);
			var disconnectTagFromEntity = GetComponentDataFromEntity<NetworkStreamDisconnected>(true);

			inputDeps = Entities
			            .ForEach((Entity entity, int nativeThreadIndex, in NetworkOwner netOwner, in GamePlayer gamePlayer) =>
			            {
				            if (!connectionFromEntity.Exists(netOwner.Value) || disconnectTagFromEntity.Exists(netOwner.Value))
					            ecb.DestroyEntity(nativeThreadIndex, entity);
			            })
			            .Schedule(inputDeps);

			m_Barrier.AddJobHandleForProducer(inputDeps);
			return inputDeps;
		}
	}
}