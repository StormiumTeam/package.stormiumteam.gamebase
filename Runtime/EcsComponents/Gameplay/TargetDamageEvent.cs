using DefaultNamespace;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace StormiumTeam.GameBase.Components
{
	public struct TargetDamageEvent : IComponentData, IEventData
	{
		public Entity Origin;
		public Entity Destination;
		public float3 Position;
		public int    Damage;

		public class Provider : BaseProviderBatch<TargetDamageEvent>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(GameEvent),
					typeof(TargetDamageEvent),
				};
			}

			public override void SetEntityData(Entity entity, TargetDamageEvent data)
			{
				EntityManager.SetComponentData(entity, data);
			}

			protected override void OnUpdate()
			{
				EntityManager.DestroyEntity(Entities.WithAll<GameEvent, TargetDamageEvent>().ToEntityQuery());

				base.OnUpdate();
			}
		}

		public class SendReplication : DefaultSendReplicateEventBase<TargetDamageEvent, TargetDamageEventReplication, TargetDamageEventRpc>
		{
		}

		public class SpawnReplication : DefaultEventReplicatedSpawnBase<TargetDamageEventReplication, TargetDamageEvent>
		{
			private ConvertGhostEntityMap m_ConvertGhostEntityMap;

			protected override void OnCreate()
			{
				base.OnCreate();
				m_ConvertGhostEntityMap = World.GetOrCreateSystem<ConvertGhostEntityMap>();
			}

			protected override void SetEventData(TargetDamageEventReplication replicated, ref TargetDamageEvent ev)
			{
				m_ConvertGhostEntityMap.HashMap.TryGetValue((int) replicated.OriginGhostId, out ev.Origin);
				m_ConvertGhostEntityMap.HashMap.TryGetValue((int) replicated.DestinationGhostId, out ev.Destination);

				ev.Damage   = replicated.Damage;
				ev.Position = (float3) replicated.Position * 0.001f;
			}
		}
	}

	public struct TargetDamageEventReplication : IReplicatedEvent
	{
		public uint OriginGhostId;
		public uint DestinationGhostId;
		public int  Damage;
		public int3 Position;

		public uint SnapshotTick   { get; set; }
		public int  SimulationTick { get; set; }
	}

	public struct TargetDamageEventRpc : IEventRpcCommand<TargetDamageEvent, TargetDamageEventReplication>
	{
		public NativeArray<TargetDamageEventReplication> ReplicatedArray;
		public uint SnapshotTick;

		public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
		{
			for (var i = 0; i != ReplicatedArray.Length; i++)
			{
				var ent = commandBuffer.CreateEntity(jobIndex);
				commandBuffer.AddComponent(jobIndex, ent, ReplicatedArray[i]);
			}

			ReplicatedArray.Dispose();
		}

		public void Serialize(DataStreamWriter writer)
		{
			using (var compression = new NetworkCompressionModel(Allocator.Temp))
			{
				writer.WritePackedUInt(SnapshotTick, compression);
				
				var count = ReplicatedArray.Length;

				writer.WritePackedInt(count, compression);
				for (var i = 0; i != count; i++)
				{
					writer.WritePackedUInt(ReplicatedArray[i].OriginGhostId, compression);
					writer.WritePackedUInt(ReplicatedArray[i].DestinationGhostId, compression);
					writer.WritePackedInt(ReplicatedArray[i].Damage, compression);
					writer.WritePackedInt(ReplicatedArray[i].Position.x, compression);
					writer.WritePackedInt(ReplicatedArray[i].Position.y, compression);
					writer.WritePackedInt(ReplicatedArray[i].Position.z, compression);
				}
			}

			writer.Flush();
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			using (var compression = new NetworkCompressionModel(Allocator.Temp))
			{
				SnapshotTick = reader.ReadPackedUInt(ref ctx, compression);

				var count = reader.ReadPackedInt(ref ctx, compression);
				ReplicatedArray = new NativeArray<TargetDamageEventReplication>(count, Allocator.Temp);

				for (var i = 0; i != count; i++)
				{
					ReplicatedArray[i] = new TargetDamageEventReplication
					{
						OriginGhostId      = reader.ReadPackedUInt(ref ctx, compression),
						DestinationGhostId = reader.ReadPackedUInt(ref ctx, compression),
						Damage             = reader.ReadPackedInt(ref ctx, compression),
						Position = new int3
						(
							reader.ReadPackedInt(ref ctx, compression),
							reader.ReadPackedInt(ref ctx, compression),
							reader.ReadPackedInt(ref ctx, compression)
						),
						SnapshotTick = SnapshotTick
					};
				}
			}
		}

		[NativeDisableContainerSafetyRestriction]
		private ComponentDataFromEntity<GhostSystemStateComponent> m_GhostStateFromEntity;

		public void Init(JobComponentSystem system)
		{
			m_GhostStateFromEntity = system.GetComponentDataFromEntity<GhostSystemStateComponent>();

			SnapshotTick = system.World.GetExistingSystem<ServerSimulationSystemGroup>().ServerTick;
		}

		public void SetData(Entity connection, NativeArray<TargetDamageEvent> events)
		{
			ReplicatedArray = new NativeArray<TargetDamageEventReplication>(events.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			for (var i = 0; i != events.Length; i++)
			{
				var ev = events[i];
				ReplicatedArray[i] = new TargetDamageEventReplication
				{
					OriginGhostId      = m_GhostStateFromEntity.GetGhostId(ev.Origin),
					DestinationGhostId = m_GhostStateFromEntity.GetGhostId(ev.Destination),
					Damage             = ev.Damage,
					Position           = (int3) (ev.Position * 1000)
				};
			}
		}
	}
}