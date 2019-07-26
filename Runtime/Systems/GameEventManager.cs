using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public struct GameEvent : IComponentData
	{
		public uint SnapshotTick;
		public int SimulationTick;
	}

	public struct ReplicatedEventTag : IComponentData
	{
	}
	
	public interface IEventData : IComponentData
	{
	}

	public interface IReplicatedEvent : IComponentData
	{
		uint SnapshotTick { get; set; }
		int SimulationTick { get; set; }
	}

	public interface IEventRpcCommand<TEvent, TReplication> : IRpcCommand
		where TEvent : struct, IEventData
		where TReplication : struct, IReplicatedEvent
	{
		void Init(JobComponentSystem system);
		void SetData(Entity connection, NativeArray<TEvent> events);
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class DefaultSendReplicateEventBase<TEvent, TReplication, TRpc> : JobComponentSystem
		where TEvent : struct, IEventData
		where TReplication : struct, IReplicatedEvent
		where TRpc : struct, IEventRpcCommand<TEvent, TReplication>
	{
		private struct JobSend : IJob
		{
			[ReadOnly]
			public NativeArray<TEvent> Events;

			public RpcQueue<TRpc> RpcQueue;
			public TRpc           RpcData;

			[ReadOnly]
			public NativeArray<Entity> Clients;

			public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> OutgoingBuffer;

			public void Execute()
			{
				for (var i = 0; i != Clients.Length; i++)
				{
					RpcData.SetData(Clients[i], Events);
					RpcQueue.Schedule(OutgoingBuffer[Clients[i]], RpcData);
				}
			}
		}

		private EntityQuery          m_EventQuery;
		private EntityQuery          m_ClientQuery;
		private RpcQueueSystem<TRpc> m_RpcQueueSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_EventQuery     = GetEntityQuery(typeof(GameEvent), typeof(TEvent));
			m_ClientQuery    = GetEntityQuery(typeof(NetworkStreamInGame), typeof(OutgoingRpcDataStreamBufferComponent));
			m_RpcQueueSystem = World.GetOrCreateSystem<RpcQueueSystem<TRpc>>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			m_EventQuery.AddDependency(inputDeps);
			m_ClientQuery.AddDependency(inputDeps);

			var rpcData = new TRpc();
			rpcData.Init(this);

			inputDeps = new JobSend
			{
				Events         = m_EventQuery.ToComponentDataArray<TEvent>(Allocator.TempJob, out var dep1),
				RpcQueue       = m_RpcQueueSystem.GetRpcQueue(),
				RpcData        = rpcData,
				Clients        = m_ClientQuery.ToEntityArray(Allocator.TempJob, out var dep2),
				OutgoingBuffer = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>()
			}.Schedule(JobHandle.CombineDependencies(inputDeps, dep1, dep2));

			return inputDeps;
		}
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(NetworkStreamReceiveSystem))]
	public abstract class DefaultEventReplicatedSpawnBase<TReplicated, TEvent> : JobComponentSystem
		where TReplicated : struct, IReplicatedEvent
		where TEvent : struct, IEventData
	{
		private EntityArchetype m_EventArchetype;
		
		private EntityQuery m_ReplicatedQuery;
		private EntityQuery m_EventQuery;

		protected override void OnCreate()
		{
			base.OnCreate();


			m_EventArchetype = GetCustomEventArchetype();
			m_EventArchetype = m_EventArchetype.Valid
				? m_EventArchetype
				: EntityManager.CreateArchetype(typeof(TReplicated), typeof(TEvent), typeof(ReplicatedEventTag));

			m_ReplicatedQuery = GetEntityQuery(typeof(TReplicated), ComponentType.Exclude<ReplicatedEventTag>());
			m_EventQuery      = GetEntityQuery(typeof(TReplicated), typeof(TEvent), typeof(ReplicatedEventTag));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (m_EventQuery.CalculateLength() > 0)
				EntityManager.DestroyEntity(m_EventQuery);

			if (m_ReplicatedQuery.CalculateLength() < 0)
				return inputDeps;

			var entities = new NativeArray<Entity>(m_ReplicatedQuery.CalculateLength(), Allocator.TempJob);
			EntityManager.CreateEntity(m_EventArchetype, entities);

			var replicatedEntities = m_ReplicatedQuery.ToEntityArray(Allocator.TempJob);
			for (var i = 0; i != entities.Length; i++)
			{
				var repl = EntityManager.GetComponentData<TReplicated>(replicatedEntities[i]);
				var data = default(TEvent);

				SetEventData(repl, ref data);

				EntityManager.SetComponentData(entities[i], data);
			}

			entities.Dispose();

			EntityManager.DestroyEntity(m_ReplicatedQuery);

			return inputDeps;
		}

		protected virtual EntityArchetype GetCustomEventArchetype()
		{
			return m_EventArchetype;
		}

		protected abstract void SetEventData(TReplicated replicated, ref TEvent ev);
	}
}