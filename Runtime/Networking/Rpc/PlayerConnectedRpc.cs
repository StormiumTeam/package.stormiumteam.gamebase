using Revolution;
using Unity.NetCode;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	[BurstCompile]
	public struct PlayerConnectedRpc : IRpcCommand
	{
		public class RequestSystem : RpcCommandRequestSystem<PlayerConnectedRpc>
		{}
		
		public int ServerId;
		
		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(ServerId);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			ServerId = reader.ReadInt(ref ctx);
		}

		[BurstCompile]
		private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
		{
			var rpcData = default(PlayerConnectedRpc);
			rpcData.Deserialize(parameters.Reader, ref parameters.ReaderContext);

			var delayed = parameters.CommandBuffer.CreateEntity(parameters.JobIndex);
			parameters.CommandBuffer.AddComponent(parameters.JobIndex, delayed, new DelayedPlayerConnection {Connection = parameters.Connection, ServerId = rpcData.ServerId});
		}

		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
		}
	}

	/* --------------------------------------------------------- *
	 * We wait for the snapshot to come to spawn GamePlayer ghosts.
	 * Once it's done, we collect all DelayedConnectEvent and
	 * create a ConnectEvent.
	 */
	public struct DelayedPlayerConnection : IComponentData
	{
		public Entity Connection;
		public int    ServerId;
	}

	public struct PlayerConnectedEvent : IComponentData
	{
		public Entity Player;
		public Entity Connection;
		public int    ServerId;
	}

	public struct GamePlayerReadyTag : IComponentData
	{
	}

	public struct WorldOwnedTag : IComponentData
	{
	}

	public struct GamePlayerLocalTag : IComponentData
	{
	}

	[UpdateAfter(typeof(SnapshotReceiveSystem))]
	public class PlayerConnectedEventCreationSystem : JobComponentSystem
	{
		[BurstCompile]
		public struct FindFirstNetworkIdJob : IJobForEach<NetworkIdComponent>
		{
			public NativeArray<NetworkIdComponent> PlayerIds;

			[BurstDiscard]
			private void NonBurst_ThrowWarning()
			{
				Debug.LogWarning("PlayerIds[0] already assigned to " + PlayerIds[0].Value);
			}

			public void Execute(ref NetworkIdComponent networkId)
			{
				if (PlayerIds[0].Value == default)
				{
					PlayerIds[0] = networkId;
				}
				else
				{
					NonBurst_ThrowWarning();
				}
			}
		}

		[RequireComponentTag(typeof(ReplicatedEntity))]
		public struct FindPlayerJob : IJobForEachWithEntity<GamePlayer>
		{
			[ReadOnly]
			public ComponentDataFromEntity<GamePlayerReadyTag> PlayerReadyTag;

			[ReadOnly]
			public NativeArray<NetworkIdComponent> PlayerIds;

			public EntityCommandBuffer.Concurrent CommandBuffer;

			[ReadOnly] public NativeArray<Entity>                  DelayedEntities;
			[ReadOnly] public NativeArray<DelayedPlayerConnection> DelayedData;

			public void Execute(Entity entity, int jobIndex, ref GamePlayer gamePlayer)
			{
				var count = DelayedEntities.Length;
				for (var ent = 0; ent != count; ent++)
				{
					if (DelayedData[ent].ServerId == gamePlayer.ServerId)
					{
						if (!PlayerReadyTag.Exists(entity))
						{
							CommandBuffer.AddComponent(jobIndex, entity, default(GamePlayerReadyTag));

							// Create connect event
							var evEnt = CommandBuffer.CreateEntity(jobIndex);
							CommandBuffer.AddComponent(jobIndex, evEnt, new PlayerConnectedEvent {Player = entity, Connection = DelayedData[ent].Connection, ServerId = gamePlayer.ServerId});
						}
						else
						{
							Debug.LogWarning($"{entity} already had a 'GamePlayerReadyTag'");
						}

						// this is our player
						if (PlayerIds.Length > 0 && PlayerIds[0].Value == gamePlayer.ServerId)
						{
							CommandBuffer.AddComponent(jobIndex, entity, default(GamePlayerLocalTag));
							CommandBuffer.AddComponent(jobIndex, entity, default(WorldOwnedTag));
							CommandBuffer.SetComponent(jobIndex, DelayedData[ent].Connection, new CommandTargetComponent {targetEntity = entity});
						}

						CommandBuffer.DestroyEntity(jobIndex, DelayedEntities[ent]);
					}
				}
			}
		}

		private struct DisposeJob : IJob
		{
			[DeallocateOnJobCompletion] public NativeArray<NetworkIdComponent>      PlayerIds;
			[DeallocateOnJobCompletion] public NativeArray<Entity>                  DelayedEntities;
			[DeallocateOnJobCompletion] public NativeArray<DelayedPlayerConnection> DelayedData;

			public void Execute()
			{

			}
		}

		private BeginSimulationEntityCommandBufferSystem m_Barrier;
		private EntityQuery                              m_DelayedQuery;
		private EntityQuery                              m_PreviousEventQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier            = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			m_DelayedQuery       = GetEntityQuery(typeof(DelayedPlayerConnection));
			m_PreviousEventQuery = GetEntityQuery(typeof(PlayerConnectedEvent));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var peLength = m_PreviousEventQuery.CalculateEntityCount();
			if (peLength > 0)
			{
				EntityManager.DestroyEntity(m_PreviousEventQuery);
			}

			var playerIds = new NativeArray<NetworkIdComponent>(1, Allocator.TempJob);
			inputDeps = new FindFirstNetworkIdJob
			{
				PlayerIds = playerIds
			}.Schedule(this, inputDeps);

			m_DelayedQuery.AddDependency(inputDeps);
			var findPlayerJob = new FindPlayerJob
			{
				PlayerReadyTag = GetComponentDataFromEntity<GamePlayerReadyTag>(),
				CommandBuffer  = m_Barrier.CreateCommandBuffer().ToConcurrent(),

				DelayedEntities = m_DelayedQuery.ToEntityArray(Allocator.TempJob, out var dep1),
				DelayedData     = m_DelayedQuery.ToComponentDataArray<DelayedPlayerConnection>(Allocator.TempJob, out var dep2),
				PlayerIds       = playerIds
			};

			inputDeps = findPlayerJob.Schedule(this, JobHandle.CombineDependencies(inputDeps, dep1, dep2));
			inputDeps = new DisposeJob
			{
				PlayerIds       = playerIds,
				DelayedEntities = findPlayerJob.DelayedEntities,
				DelayedData     = findPlayerJob.DelayedData
			}.Schedule(inputDeps);

			m_Barrier.AddJobHandleForProducer(inputDeps);
			m_DelayedQuery.CompleteDependency();

			//inputDeps.Complete();

			return inputDeps;
		}
	}
}