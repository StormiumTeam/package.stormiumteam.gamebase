using Runtime.EcsComponents;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public struct ClientLoadedRpc : IRpcCommand
	{
		public int GameVersion;
		
		public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
		{
			commandBuffer.AddComponent(jobIndex, connection, new NetworkStreamInGame());

			var gamePlayerCreate = commandBuffer.CreateEntity(jobIndex);
			commandBuffer.AddComponent(jobIndex, gamePlayerCreate, new CreateGamePlayer
			{
				Connection = connection,
				GameVersion = GameVersion,
			});
		}

		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(GameVersion);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			var byteRead = reader.GetBytesRead(ref ctx);
			if (reader.Length - byteRead < sizeof(int))
			{
				GameVersion = -1;
				return;
			}
			
			GameVersion = reader.ReadInt(ref ctx);
		}
	}

	public struct CreateGamePlayer : IComponentData
	{
		public Entity Connection;
		public int GameVersion;
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class CreateGamePlayerSystem : JobComponentSystem
	{
		private struct CreateJob : IJobForEachWithEntity<CreateGamePlayer>
		{
			[NativeDisableParallelForRestriction]
			public NativeList<PlayerConnectedRpc> PreMadeEvents;

			public EntityCommandBuffer.Concurrent CommandBuffer;

			public EntityArchetype PlayerArchetype;

			public int CurrentVersion;

			public UdpNetworkDriver Driver;

			[ReadOnly]
			public ComponentDataFromEntity<NetworkIdComponent> NetworkIdFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<NetworkStreamConnection> NetworkStreamConnectionFromEntity;

			public void Execute(Entity entity, int jobIndex, ref CreateGamePlayer create)
			{
				CommandBuffer.DestroyEntity(jobIndex, entity);

				if (create.GameVersion != CurrentVersion)
				{
					Debug.Log($"bye bye [player version:{create.GameVersion}, current: {CurrentVersion}]");
					NetworkStreamConnectionFromEntity[create.Connection].Value.Disconnect(Driver);
					return;
				}
				
				var networkId = NetworkIdFromEntity[create.Connection];

				var geEnt = CommandBuffer.CreateEntity(jobIndex, PlayerArchetype);
				CommandBuffer.SetComponent(jobIndex, geEnt, new GamePlayer(0, false) {ServerId = networkId.Value});
				CommandBuffer.AddComponent(jobIndex, geEnt, new NetworkOwner {Value            = create.Connection});
				CommandBuffer.AddComponent(jobIndex, geEnt, new GamePlayerReadyTag());
				CommandBuffer.AddComponent(jobIndex, geEnt, new GhostComponent());

				PreMadeEvents.Add(new PlayerConnectedRpc
				{
					ServerId = networkId.Value
				});

				// Create event
				var evEnt = CommandBuffer.CreateEntity(jobIndex);
				CommandBuffer.AddComponent(jobIndex, evEnt, new PlayerConnectedEvent {Player = geEnt, Connection = create.Connection, ServerId = networkId.Value});
			}
		}

		[RequireComponentTag(typeof(NetworkStreamInGame))]
		private struct SendRpcToConnectionsJob : IJobForEachWithEntity<NetworkStreamConnection>
		{
			[NativeDisableParallelForRestriction]
			public NativeList<PlayerConnectedRpc> PreMadeEvents;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> OutgoingDataFromEntity;

			public RpcQueue<PlayerConnectedRpc> RpcQueue;

			public void Execute(Entity entity, int jobIndex, ref NetworkStreamConnection connection)
			{
				for (var ev = 0; ev != PreMadeEvents.Length; ev++)
				{
					RpcQueue.Schedule(OutgoingDataFromEntity[entity], PreMadeEvents[ev]);
				}
			}
		}

		private BeginSimulationEntityCommandBufferSystem m_Barrier;
		private EntityQuery                              m_PreviousEventQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier            = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			m_PreviousEventQuery = GetEntityQuery(typeof(PlayerConnectedEvent));

			GetEntityQuery(typeof(CreateGamePlayer));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var peLength = m_PreviousEventQuery.CalculateLength();
			if (peLength > 0)
			{
				EntityManager.DestroyEntity(m_PreviousEventQuery);
			}

			var preMadeEvents = new NativeList<PlayerConnectedRpc>(Allocator.TempJob);
			inputDeps = new CreateJob
			{
				PreMadeEvents = preMadeEvents,
				CurrentVersion = GameStatic.Version,
				
				Driver = World.GetExistingSystem<NetworkStreamReceiveSystem>().Driver,
				NetworkStreamConnectionFromEntity = GetComponentDataFromEntity<NetworkStreamConnection>(true),

				CommandBuffer       = m_Barrier.CreateCommandBuffer().ToConcurrent(),
				PlayerArchetype = World.GetOrCreateSystem<GamePlayerProvider>().EntityArchetype,
				NetworkIdFromEntity = GetComponentDataFromEntity<NetworkIdComponent>()
			}.ScheduleSingle(this, inputDeps);
			inputDeps = new SendRpcToConnectionsJob
			{
				PreMadeEvents          = preMadeEvents,
				OutgoingDataFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>(),
				RpcQueue               = World.GetExistingSystem<RpcQueueSystem<PlayerConnectedRpc>>().GetRpcQueue()
			}.Schedule(this, inputDeps);
			inputDeps = preMadeEvents.Dispose(inputDeps); // dispose list after the end of jobs

			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}