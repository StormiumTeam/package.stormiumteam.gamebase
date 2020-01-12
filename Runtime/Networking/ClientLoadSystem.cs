using EcsComponents.MasterServer;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class ClientLoadSystem : JobComponentSystem
	{
		private EndSimulationEntityCommandBufferSystem m_Barrier;

		public NativeString64 ConnectionToken;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			ulong targetUserId = 0;
			if (HasSingleton<ConnectedMasterServerClient>())
				targetUserId = GetSingleton<ConnectedMasterServerClient>().UserId;

			var ecb     = m_Barrier.CreateCommandBuffer().ToConcurrent();
			var version = GameStatic.Version;
			var token   = ConnectionToken;
			inputDeps = Entities
			            .WithNone<NetworkStreamInGame>()
			            .ForEach((Entity connection, int nativeThreadIndex, ref NetworkIdComponent id) =>
			            {
				            ecb.AddComponent(nativeThreadIndex, connection, default(NetworkStreamInGame));

				            var reqEnt = ecb.CreateEntity(nativeThreadIndex);
				            ecb.AddComponent(nativeThreadIndex, reqEnt, new ClientLoadedRpc
				            {
					            GameVersion        = version,
					            ConnectionToken    = token,
					            MasterServerUserId = targetUserId
				            });
				            ecb.AddComponent(nativeThreadIndex, reqEnt, new SendRpcCommandRequestComponent {TargetConnection = connection});
			            }).Schedule(inputDeps);

			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}