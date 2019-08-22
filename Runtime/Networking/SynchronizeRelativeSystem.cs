using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	[UpdateBefore(typeof(AddNetworkIdSystem))]
	public class SynchronizeRelativeSystemGroup : ComponentSystemGroup
	{
		public struct ClientSyncedTag : IComponentData
		{
		}
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(GhostSpawnSystemGroup))]
	[UpdateAfter(typeof(PreConvertSystemGroup))]
	public class ReceiveRelativeSystemGroup : ComponentSystemGroup
	{
	}

	public struct ExcludeRelativeSynchronization : IComponentData
	{
	}

	public struct ClearAllRelativeTag : IComponentData
	{
	}

	public struct RelativeComponent : ISharedComponentData
	{
		public int ComponentId;
	}

	public struct DeleteRelativeCommand : IBufferElementData
	{
		public int GhostId;
	}

	public struct ReceiveRelativeCommand : IBufferElementData
	{
		public int GhostId;
		public int RelativeId;
	}


	[UpdateInGroup(typeof(SynchronizeRelativeSystemGroup))]
	[AlwaysUpdateSystem]
	public class SynchronizeRelativeSystem<TRelative> : JobComponentSystem
		where TRelative : struct, IEntityDescription
	{
		public struct Pair
		{
			public Entity entity;
			public int    ghostId;
		}

		public struct SendAllRpc : IRpcCommand
		{
			public NativeArray<int> GhostIds;
			public NativeArray<int> RelativeIds;

			public NetworkCompressionModel NetworkCompressionModel;

			public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
			{
				var ent    = commandBuffer.CreateEntity(jobIndex);
				var buffer = commandBuffer.AddBuffer<ReceiveRelativeCommand>(jobIndex, ent);
				var length = GhostIds.Length;
				for (var i = 0; i != length; i++)
				{
					if (RelativeIds[i] <= 0)
						continue;

					buffer.Add(new ReceiveRelativeCommand
					{
						GhostId    = GhostIds[i],
						RelativeId = RelativeIds[i]
					});
				}

				commandBuffer.AddSharedComponent(jobIndex, ent, new RelativeComponent {ComponentId = TypeManager.GetTypeIndex<Relative<TRelative>>()});
				commandBuffer.AddComponent(jobIndex, ent, typeof(ClearAllRelativeTag));
			}

			public void Serialize(DataStreamWriter writer)
			{
				ref var compression = ref NetworkCompressionModel;

				var length = GhostIds.Length;
				writer.WritePackedUInt((uint) length, compression);
				for (var i = 0; i != length; i++)
				{
					writer.WritePackedInt(GhostIds[i], compression);
					writer.WritePackedInt(RelativeIds[i], compression);
				}

				writer.Flush();
			}

			public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
			{
				using (var compression = new NetworkCompressionModel(Allocator.Temp))
				{
					var length = (int) reader.ReadPackedUInt(ref ctx, compression);
					GhostIds    = new NativeArray<int>(length, Allocator.Temp);
					RelativeIds = new NativeArray<int>(length, Allocator.Temp);
					for (var i = 0; i != length; i++)
					{
						GhostIds[i]    = reader.ReadPackedInt(ref ctx, compression);
						RelativeIds[i] = reader.ReadPackedInt(ref ctx, compression);
					}
				}
			}
		}

		public struct SendDeltaRpc : IRpcCommand
		{
			public NativeArray<int> DeletedGhostIds;
			public NativeArray<int> AddedGhostIds;
			public NativeArray<int> RelativeIds;

			public NetworkCompressionModel NetworkCompressionModel;

			public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
			{
				var ent       = commandBuffer.CreateEntity(jobIndex);
				var remBuffer = commandBuffer.AddBuffer<DeleteRelativeCommand>(jobIndex, ent).Reinterpret<int>();
				var addBuffer = commandBuffer.AddBuffer<ReceiveRelativeCommand>(jobIndex, ent);

				remBuffer.AddRange(DeletedGhostIds);

				for (int i = 0, length = AddedGhostIds.Length; i != length; i++)
				{
					if (RelativeIds[i] <= 0)
						continue;

					addBuffer.Add(new ReceiveRelativeCommand
					{
						GhostId    = AddedGhostIds[i],
						RelativeId = RelativeIds[i]
					});
				}

				commandBuffer.AddSharedComponent(jobIndex, ent, new RelativeComponent {ComponentId = TypeManager.GetTypeIndex<Relative<TRelative>>()});
			}

			public void Serialize(DataStreamWriter writer)
			{
				int     length      = default;
				ref var compression = ref NetworkCompressionModel;

				// Destroyed
				length = DeletedGhostIds.Length;
				writer.WritePackedUInt((uint) length, compression);
				for (var i = 0; i != length; i++)
				{
					writer.WritePackedInt(DeletedGhostIds[i], compression);
				}

				// Added
				length = AddedGhostIds.Length;
				writer.WritePackedUInt((uint) length, compression);
				for (var i = 0; i != length; i++)
				{
					writer.WritePackedInt(AddedGhostIds[i], compression);
					writer.WritePackedInt(RelativeIds[i], compression);
				}

				writer.Flush();
			}

			public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
			{
				using (var compression = new NetworkCompressionModel(Allocator.Temp))
				{
					int length = default;

					// Destroyed
					length          = (int) reader.ReadPackedUInt(ref ctx, compression);
					DeletedGhostIds = new NativeArray<int>(length, Allocator.Temp);
					for (var i = 0; i != length; i++)
					{
						DeletedGhostIds[i] = reader.ReadPackedInt(ref ctx, compression);
					}

					// Added
					length        = (int) reader.ReadPackedUInt(ref ctx, compression);
					AddedGhostIds = new NativeArray<int>(length, Allocator.Temp);
					RelativeIds   = new NativeArray<int>(length, Allocator.Temp);
					for (var i = 0; i != length; i++)
					{
						AddedGhostIds[i] = reader.ReadPackedInt(ref ctx, compression);
						RelativeIds[i]   = reader.ReadPackedInt(ref ctx, compression);
					}
				}
			}
		}

		public struct SendUpdateRpc : IRpcCommand
		{
			public NativeArray<int> GhostIds;
			public NativeArray<int> RelativeIds;

			public NetworkCompressionModel NetworkCompressionModel;

			public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
			{
				var ent       = commandBuffer.CreateEntity(jobIndex);
				var remBuffer = commandBuffer.AddBuffer<DeleteRelativeCommand>(jobIndex, ent).Reinterpret<int>();
				var addBuffer = commandBuffer.AddBuffer<ReceiveRelativeCommand>(jobIndex, ent);

				for (int i = 0, length = GhostIds.Length; i != length; i++)
				{
					if (RelativeIds[i] <= 0)
						continue;

					addBuffer.Add(new ReceiveRelativeCommand
					{
						GhostId    = GhostIds[i],
						RelativeId = RelativeIds[i]
					});
				}

				commandBuffer.AddSharedComponent(jobIndex, ent, new RelativeComponent {ComponentId = TypeManager.GetTypeIndex<Relative<TRelative>>()});
			}

			public void Serialize(DataStreamWriter writer)
			{
				ref var compression = ref NetworkCompressionModel;
				int     length      = default;

				length = GhostIds.Length;
				writer.WritePackedUInt((uint) length, compression);
				for (var i = 0; i != length; i++)
				{
					writer.WritePackedInt(GhostIds[i], compression);
					writer.WritePackedInt(RelativeIds[i], compression);
				}

				writer.Flush();
			}

			public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
			{
				using (var compression = new NetworkCompressionModel(Allocator.Temp))
				{
					int length = default;

					length      = (int) reader.ReadPackedUInt(ref ctx, compression);
					GhostIds    = new NativeArray<int>(length, Allocator.Temp);
					RelativeIds = new NativeArray<int>(length, Allocator.Temp);
					for (var i = 0; i != length; i++)
					{
						GhostIds[i]    = reader.ReadPackedInt(ref ctx, compression);
						RelativeIds[i] = reader.ReadPackedInt(ref ctx, compression);
					}
				}
			}
		}

		private struct ClearLists : IJob
		{
			public NativeList<Pair> AddList;
			public NativeList<Pair> UpdateList;

			public void Execute()
			{
				AddList.Clear();
				UpdateList.Clear();
			}
		}

		[RequireComponentTag(typeof(GhostComponent))]
		[ExcludeComponent(typeof(ExcludeRelativeSynchronization))]
		[BurstCompile]
		private struct FindGhostWithRelative : IJobForEachWithEntity<Relative<TRelative>, GhostSystemStateComponent>
		{
			public NativeList<Entity> Entities;
			public NativeList<int>    GhostIds;

			public NativeList<Pair> AddList;
			public NativeList<Pair> UpdateList;

			public NativeHashMap<Entity, Entity> EntityToRelative;

			public void Execute(Entity entity, int index, [ReadOnly] ref Relative<TRelative> relative, ref GhostSystemStateComponent ghostSystemState)
			{
				var length = Entities.Length;
				for (var i = 0; i != length; i++)
				{
					if (Entities[i] == entity)
					{
						if (!EntityToRelative.TryGetValue(entity, out var previousRelative)
						    || previousRelative != relative.Target)
						{
							EntityToRelative[entity] = relative.Target;

							UpdateList.Add(new Pair {entity = entity, ghostId = ghostSystemState.ghostId});
						}

						return;
					}
				}

				Entities.Add(entity);
				GhostIds.Add(ghostSystemState.ghostId);
				AddList.Add(new Pair {entity = entity, ghostId = ghostSystemState.ghostId});
			}
		}

		[BurstCompile]
		private struct SeekAndDestroyJob : IJob
		{
			public NativeList<Entity> Entities;
			public NativeList<int>    GhostIds;

			public NativeList<Pair> DestroyList;

			public NativeHashMap<Entity, Entity> EntityToRelative;


			[ReadOnly] public ComponentDataFromEntity<Relative<TRelative>>            RelativeFromEntity;
			[ReadOnly] public ComponentDataFromEntity<ExcludeRelativeSynchronization> ExcludeFromEntity;

			public void Execute()
			{
				DestroyList.Clear();

				for (var i = 0; i != Entities.Length; i++)
				{
					if (RelativeFromEntity.Exists(Entities[i]) || ExcludeFromEntity.Exists(Entities[i]))
						continue;

					DestroyList.Add(new Pair {entity = Entities[i], ghostId = GhostIds[i]});

					if (EntityToRelative.ContainsKey(Entities[i]))
						EntityToRelative.Remove(Entities[i]);

					Entities.RemoveAtSwapBack(i);
					GhostIds.RemoveAtSwapBack(i);
					i--;
				}
			}
		}
		
		//[BurstCompile] not burstable yet
		private struct SendFullRpcJob : IJobForEachWithEntity_EB<OutgoingRpcDataStreamBufferComponent>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			public NativeArray<Entity>            Entities;
			public NativeList<int>                GhostIds;

			[ReadOnly] public ComponentDataFromEntity<Relative<TRelative>>       RelativeFromEntity;
			[ReadOnly] public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;

			public RpcQueue<SendAllRpc>    SendAllRpcQueue;
			public NetworkCompressionModel NetworkCompressionModel;

			public void Execute(Entity entity, int index, DynamicBuffer<OutgoingRpcDataStreamBufferComponent> outgoingData)
			{
				CommandBuffer.AddComponent(index, entity, typeof(SynchronizeRelativeSystemGroup.ClientSyncedTag));

				var relativeIds = new NativeArray<int>(GhostIds.Length, Allocator.Temp);
				for (int i = 0, length = Entities.Length; i < length; i++)
				{
					if (!RelativeFromEntity.Exists(Entities[i]))
						continue;
					var relative = RelativeFromEntity[Entities[i]];
					if (relative.Target == default || !GhostStateFromEntity.Exists(relative.Target))
						continue;

					relativeIds[i] = GhostStateFromEntity[relative.Target].ghostId;
				}

				var rpcCall = new SendAllRpc
				{
					GhostIds                = GhostIds,
					RelativeIds             = relativeIds,
					NetworkCompressionModel = NetworkCompressionModel
				};
				SendAllRpcQueue.Schedule(outgoingData, rpcCall);
			}
		}

		[RequireComponentTag(typeof(SynchronizeRelativeSystemGroup.ClientSyncedTag))]
		[BurstCompile]
		private struct SendDeltaRpcJob : IJobForEach_B<OutgoingRpcDataStreamBufferComponent>
		{
			public NativeList<Pair> DestroyList;
			public NativeList<Pair> AddList;

			[ReadOnly] public ComponentDataFromEntity<Relative<TRelative>>       RelativeFromEntity;
			[ReadOnly] public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;

			public RpcQueue<SendDeltaRpc>  SendDeltaRpcQueue;
			public NetworkCompressionModel NetworkCompressionModel;

			public void Execute(DynamicBuffer<OutgoingRpcDataStreamBufferComponent> outgoingData)
			{
				if (DestroyList.Length <= 0 && AddList.Length <= 0)
					return;

				var relativeIds = new NativeArray<int>(AddList.Length, Allocator.Temp);
				for (int i = 0, length = AddList.Length; i < length; i++)
				{
					if (!RelativeFromEntity.Exists(AddList[i].entity))
						continue;
					var relative = RelativeFromEntity[AddList[i].entity];
					if (relative.Target == default || !GhostStateFromEntity.Exists(relative.Target))
						continue;

					relativeIds[i] = GhostStateFromEntity[relative.Target].ghostId;
				}

				var deletedGhostIds = new NativeArray<int>(DestroyList.Length, Allocator.Temp);
				var addedGhostIds   = new NativeArray<int>(AddList.Length, Allocator.Temp);
				for (int i = 0, length = deletedGhostIds.Length; i < length; i++)
				{
					deletedGhostIds[i] = DestroyList[i].ghostId;
				}

				for (int i = 0, length = addedGhostIds.Length; i < length; i++)
				{
					addedGhostIds[i] = AddList[i].ghostId;
				}

				var rpcCall = new SendDeltaRpc
				{
					DeletedGhostIds         = deletedGhostIds,
					AddedGhostIds           = addedGhostIds,
					RelativeIds             = relativeIds,
					NetworkCompressionModel = NetworkCompressionModel
				};
				SendDeltaRpcQueue.Schedule(outgoingData, rpcCall);
			}
		}

		private struct SendUpdateRpcJob : IJobForEach_B<OutgoingRpcDataStreamBufferComponent>
		{
			public NativeList<Pair> UpdateList;

			[ReadOnly] public ComponentDataFromEntity<Relative<TRelative>>       RelativeFromEntity;
			[ReadOnly] public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;

			public RpcQueue<SendUpdateRpc> SendUpdateRpcQueue;
			public NetworkCompressionModel NetworkCompressionModel;

			public void Execute(DynamicBuffer<OutgoingRpcDataStreamBufferComponent> outgoingData)
			{
				if (UpdateList.Length <= 0)
					return;

				var relativeIds = new NativeArray<int>(UpdateList.Length, Allocator.Temp);
				for (int i = 0, length = UpdateList.Length; i < length; i++)
				{
					if (!RelativeFromEntity.Exists(UpdateList[i].entity))
						continue;
					var relative = RelativeFromEntity[UpdateList[i].entity];
					if (relative.Target == default || !GhostStateFromEntity.Exists(relative.Target))
						continue;

					relativeIds[i] = GhostStateFromEntity[relative.Target].ghostId;
				}

				var ghostIds = new NativeArray<int>(UpdateList.Length, Allocator.Temp);
				for (int i = 0, length = ghostIds.Length; i < length; i++)
				{
					ghostIds[i] = UpdateList[i].ghostId;
				}

				var rpcCall = new SendUpdateRpc
				{
					GhostIds                = ghostIds,
					RelativeIds             = relativeIds,
					NetworkCompressionModel = NetworkCompressionModel
				};
				SendUpdateRpcQueue.Schedule(outgoingData, rpcCall);
			}
		}

		private EntityCommandBufferSystem     m_EndBarrier;
		private RpcQueueSystem<SendAllRpc>    m_SendAllRpcQueueSystem;
		private RpcQueueSystem<SendDeltaRpc>  m_SendDeltaRpcQueueSystem;
		private RpcQueueSystem<SendUpdateRpc> m_SendUpdateRpcQueueSystem;

		private NativeHashMap<Entity, Entity> EntityToRelative;

		private NativeList<Entity> Entities;
		private NativeList<int>    GhostIds;

		private NativeList<Pair> AddList;
		private NativeList<Pair> UpdateList;
		private NativeList<Pair> DestroyList;

		private NetworkCompressionModel m_NetworkCompressionModel;

		private EntityQuery m_GhostQuery;
		private EntityQuery m_SendDataWithoutSyncQuery;
		private EntityQuery m_SendDataQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_EndBarrier               = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
			m_SendAllRpcQueueSystem    = World.GetOrCreateSystem<RpcQueueSystem<SendAllRpc>>();
			m_SendDeltaRpcQueueSystem  = World.GetOrCreateSystem<RpcQueueSystem<SendDeltaRpc>>();
			m_SendUpdateRpcQueueSystem = World.GetOrCreateSystem<RpcQueueSystem<SendUpdateRpc>>();

			m_NetworkCompressionModel = new NetworkCompressionModel(Allocator.Persistent);
			EntityToRelative          = new NativeHashMap<Entity, Entity>(128, Allocator.Persistent);

			m_GhostQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(GhostComponent), typeof(GhostSystemStateComponent), typeof(Relative<TRelative>)},
				None = new ComponentType[] {typeof(ExcludeRelativeSynchronization)}
			});
			m_SendDataWithoutSyncQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(NetworkStreamInGame), typeof(OutgoingRpcDataStreamBufferComponent)},
				None = new ComponentType[] {typeof(SynchronizeRelativeSystemGroup.ClientSyncedTag)}
			});
			m_SendDataQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(NetworkStreamInGame), typeof(OutgoingRpcDataStreamBufferComponent)}
			});

			Entities = new NativeList<Entity>(128, Allocator.Persistent);
			GhostIds = new NativeList<int>(128, Allocator.Persistent);

			AddList     = new NativeList<Pair>(128, Allocator.Persistent);
			UpdateList  = new NativeList<Pair>(128, Allocator.Persistent);
			DestroyList = new NativeList<Pair>(128, Allocator.Persistent);
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (AddList.Length > 0 && UpdateList.Length > 0)
			{
				inputDeps = new ClearLists
				{
					AddList    = AddList,
					UpdateList = UpdateList
				}.Schedule(inputDeps);
			}

			inputDeps = new FindGhostWithRelative
			{
				Entities         = Entities,
				GhostIds         = GhostIds,
				AddList          = AddList,
				UpdateList       = UpdateList,
				EntityToRelative = EntityToRelative
			}.ScheduleSingle(m_GhostQuery, inputDeps);
			
			if (Entities.Length > 0)
			{
				inputDeps = new SeekAndDestroyJob
				{
					Entities           = Entities,
					GhostIds           = GhostIds,
					DestroyList        = DestroyList,
					EntityToRelative   = EntityToRelative,
					RelativeFromEntity = GetComponentDataFromEntity<Relative<TRelative>>(true),
					ExcludeFromEntity  = GetComponentDataFromEntity<ExcludeRelativeSynchronization>(true),
				}.Schedule(inputDeps);
			}

			var relativeFromEntity = GetComponentDataFromEntity<Relative<TRelative>>(true);
			var ghostStateFromEntity = GetComponentDataFromEntity<GhostSystemStateComponent>(true);
			if (m_SendDataWithoutSyncQuery.CalculateEntityCount() > 0)
			{
				inputDeps = new SendFullRpcJob
				{
					CommandBuffer           = m_EndBarrier.CreateCommandBuffer().ToConcurrent(),
					Entities                = Entities,
					GhostIds                = GhostIds,
					RelativeFromEntity      = relativeFromEntity,
					GhostStateFromEntity    = ghostStateFromEntity,
					SendAllRpcQueue         = m_SendAllRpcQueueSystem.GetRpcQueue(),
					NetworkCompressionModel = m_NetworkCompressionModel,
				}.ScheduleSingle(m_SendDataWithoutSyncQuery, inputDeps);
			}

			if (m_SendDataQuery.CalculateEntityCount() > 0)
			{
				inputDeps.Complete();

				if (DestroyList.Length > 0 || AddList.Length > 0)
				{
					inputDeps = new SendDeltaRpcJob
					{
						DestroyList             = DestroyList,
						AddList                 = AddList,
						RelativeFromEntity      = relativeFromEntity,
						GhostStateFromEntity    = ghostStateFromEntity,
						SendDeltaRpcQueue       = m_SendDeltaRpcQueueSystem.GetRpcQueue(),
						NetworkCompressionModel = m_NetworkCompressionModel,
					}.ScheduleSingle(m_SendDataQuery, inputDeps);
				}

				if (UpdateList.Length > 0)
				{
					inputDeps = new SendUpdateRpcJob
					{
						UpdateList              = UpdateList,
						RelativeFromEntity      = relativeFromEntity,
						GhostStateFromEntity    = ghostStateFromEntity,
						SendUpdateRpcQueue      = m_SendUpdateRpcQueueSystem.GetRpcQueue(),
						NetworkCompressionModel = m_NetworkCompressionModel,
					}.ScheduleSingle(m_SendDataQuery, inputDeps);
				}
			}

			m_EndBarrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			m_NetworkCompressionModel.Dispose();
		}
	}

	[UpdateInGroup(typeof(ReceiveRelativeSystemGroup))]
	[AlwaysUpdateSystem]
	public class ReceiveRelativeSystem<TRelative> : ComponentSystem
		where TRelative : struct, IEntityDescription
	{
		private struct PairConverted
		{
			public Entity Entity;
			public Entity Relative;
		}

		private struct Delayed
		{
			public int RemoveTick;
			public int GhostId;
			public int RelativeId;
		}

		private int         m_TypeIndex;
		private EntityQuery m_GhostWithRelativeQuery;
		private EntityQuery m_ClearAllRelativeQuery;
		private EntityQuery m_DeleteRelativeQuery;
		private EntityQuery m_ReceiveRelativeQuery;

		private NativeList<Delayed> m_DelayedQueue;

		private struct ConvertPairsToEntities : IJobParallelFor
		{
			[ReadOnly]  public NativeHashMap<int, Entity>          HashMap;
			[ReadOnly]  public NativeArray<ReceiveRelativeCommand> Pairs;
			[WriteOnly] public NativeArray<PairConverted>          Converteds;

			public void Execute(int index)
			{
				PairConverted converted;
				HashMap.TryGetValue(Pairs[index].GhostId, out converted.Entity);
				HashMap.TryGetValue(Pairs[index].RelativeId, out converted.Relative);

				Converteds[index] = converted;
			}
		}

		private struct ConvertToEntities : IJobParallelFor
		{
			[ReadOnly]  public NativeHashMap<int, Entity>         HashMap;
			[ReadOnly]  public NativeArray<DeleteRelativeCommand> GhostIds;
			[WriteOnly] public NativeArray<Entity>                Converteds;

			public void Execute(int index)
			{
				Entity entity;
				HashMap.TryGetValue(GhostIds[index].GhostId, out entity);

				Converteds[index] = entity;
			}
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			m_TypeIndex              = TypeManager.GetTypeIndex<Relative<TRelative>>();
			m_GhostWithRelativeQuery = GetEntityQuery(typeof(ReplicatedEntityComponent), typeof(Relative<TRelative>));
			m_ClearAllRelativeQuery  = GetEntityQuery(typeof(RelativeComponent), typeof(ClearAllRelativeTag));
			m_ReceiveRelativeQuery   = GetEntityQuery(typeof(RelativeComponent), typeof(ReceiveRelativeCommand));
			m_DeleteRelativeQuery    = GetEntityQuery(typeof(RelativeComponent), typeof(DeleteRelativeCommand));
			m_DelayedQueue           = new NativeList<Delayed>(Allocator.Persistent);
		}

		protected override void OnUpdate()
		{
			JobHandle inputDeps = default;

			var convertGhostEntityMapSystem = World.GetExistingSystem<ConvertGhostEntityMap>();

			if (!m_ClearAllRelativeQuery.IsEmptyIgnoreFilter)
				m_ClearAllRelativeQuery.SetFilter(new RelativeComponent {ComponentId = m_TypeIndex});
			if (!m_ReceiveRelativeQuery.IsEmptyIgnoreFilter)
				m_ReceiveRelativeQuery.SetFilter(new RelativeComponent {ComponentId = m_TypeIndex});
			if (!m_DeleteRelativeQuery.IsEmptyIgnoreFilter)
				m_DeleteRelativeQuery.SetFilter(new RelativeComponent {ComponentId = m_TypeIndex});

			if (!m_ClearAllRelativeQuery.IsEmptyIgnoreFilter)
			{
				EntityManager.RemoveComponent(m_GhostWithRelativeQuery, typeof(Relative<TRelative>));
			}

			NativeArray<Entity>                deleteAsEntities  = default;
			NativeArray<PairConverted>         receiveAsEntities = default;
			NativeList<ReceiveRelativeCommand> receiveList       = default;
			if (m_DeleteRelativeQuery.CalculateEntityCount() > 0)
			{
				NativeList<DeleteRelativeCommand> deleteList;
				using (var chunks = m_DeleteRelativeQuery.CreateArchetypeChunkArray(Allocator.TempJob))
				{
					deleteList = Delete(chunks);
				}

				deleteAsEntities = new NativeArray<Entity>(deleteList.Length, Allocator.TempJob);
				inputDeps = new ConvertToEntities
				{
					HashMap    = convertGhostEntityMapSystem.HashMap,
					Converteds = deleteAsEntities,
					GhostIds   = deleteList
				}.Schedule(deleteList.Length, 64, JobHandle.CombineDependencies(inputDeps, convertGhostEntityMapSystem.dependency));
			}

			if (m_ReceiveRelativeQuery.CalculateEntityCount() > 0)
			{
				using (var chunks = m_ReceiveRelativeQuery.CreateArchetypeChunkArray(Allocator.TempJob))
				{
					receiveList = Receive(chunks);
				}

				receiveAsEntities = new NativeArray<PairConverted>(receiveList.Length, Allocator.TempJob);
				inputDeps = new ConvertPairsToEntities
				{
					HashMap    = convertGhostEntityMapSystem.HashMap,
					Converteds = receiveAsEntities,
					Pairs      = receiveList
				}.Schedule(receiveList.Length, 64, JobHandle.CombineDependencies(inputDeps, convertGhostEntityMapSystem.dependency));
			}

			if (m_ClearAllRelativeQuery.CalculateEntityCount() > 0)
				EntityManager.DestroyEntity(m_ClearAllRelativeQuery);
			if (m_DeleteRelativeQuery.CalculateEntityCount() > 0)
				EntityManager.DestroyEntity(m_DeleteRelativeQuery);
			if (m_ReceiveRelativeQuery.CalculateEntityCount() > 0)
				EntityManager.DestroyEntity(m_ReceiveRelativeQuery);

			inputDeps.Complete();

			if (deleteAsEntities.Length > 0)
			{
				EntityManager.RemoveComponent(deleteAsEntities, typeof(Relative<TRelative>));
				deleteAsEntities.Dispose();
			}

			if (m_DelayedQueue.Length > 0)
			{
				receiveList = new NativeList<ReceiveRelativeCommand>(Allocator.TempJob);
				for (int i = 0, length = m_DelayedQueue.Length; i < length; i++)
				{
					receiveList.Add(new ReceiveRelativeCommand
					{
						GhostId    = m_DelayedQueue[i].GhostId,
						RelativeId = m_DelayedQueue[i].RelativeId
					});
				}

				receiveAsEntities = new NativeArray<PairConverted>(receiveList.Length, Allocator.TempJob);
				inputDeps = new ConvertPairsToEntities
				{
					HashMap    = convertGhostEntityMapSystem.HashMap,
					Converteds = receiveAsEntities,
					Pairs      = receiveList
				}.Schedule(receiveList.Length, 64, inputDeps);
				inputDeps.Complete();

				m_DelayedQueue.Clear();
				for (var ent = 0; ent != receiveAsEntities.Length; ent++)
				{
					if (receiveAsEntities[ent].Entity == default || receiveAsEntities[ent].Relative == default)
					{
						m_DelayedQueue.Add(new Delayed
						{
							GhostId    = receiveList[ent].GhostId,
							RelativeId = receiveList[ent].RelativeId
						});
						continue;
					}

					EntityManager.SetOrAddComponentData(receiveAsEntities[ent].Entity, new Relative<TRelative>
					{
						Target = receiveAsEntities[ent].Relative
					});
				}

				receiveAsEntities.Dispose();
			}

			if (receiveAsEntities.Length > 0)
			{
				for (var ent = 0; ent != receiveAsEntities.Length; ent++)
				{
					if (receiveAsEntities[ent].Entity == default || receiveAsEntities[ent].Relative == default)
					{
						m_DelayedQueue.Add(new Delayed
						{
							GhostId    = receiveList[ent].GhostId,
							RelativeId = receiveList[ent].RelativeId
						});
						continue;
					}

					EntityManager.SetOrAddComponentData(receiveAsEntities[ent].Entity, new Relative<TRelative>
					{
						Target = receiveAsEntities[ent].Relative
					});
				}

				receiveAsEntities.Dispose();
				receiveList.Dispose();
			}
		}

		private NativeList<ReceiveRelativeCommand> Receive(NativeArray<ArchetypeChunk> chunks)
		{
			int ch   = 0, ent = 0;
			var list = new NativeList<ReceiveRelativeCommand>(Allocator.Temp);

			for (ch = 0; ch != chunks.Length; ch++)
			{
				var receiveBufferArray = chunks[ch].GetBufferAccessor(GetArchetypeChunkBufferType<ReceiveRelativeCommand>());
				var count              = chunks[ch].Count;
				for (ent = 0; ent != count; ent++)
				{
					var receiveBuffer = receiveBufferArray[ent];
					list.AddRange(receiveBuffer.AsNativeArray());
				}
			}

			return list;
		}

		private NativeList<DeleteRelativeCommand> Delete(NativeArray<ArchetypeChunk> chunks)
		{
			int ch, ent;
			var list = new NativeList<DeleteRelativeCommand>(Allocator.Temp);

			for (ch = 0; ch != chunks.Length; ch++)
			{
				var deleteBufferArray = chunks[ch].GetBufferAccessor(GetArchetypeChunkBufferType<DeleteRelativeCommand>());
				var count             = chunks[ch].Count;
				for (ent = 0; ent != count; ent++)
				{
					var deleteBuffer = deleteBufferArray[ent];
					list.AddRange(deleteBuffer.AsNativeArray());
				}
			}

			return list;
		}
	}
}