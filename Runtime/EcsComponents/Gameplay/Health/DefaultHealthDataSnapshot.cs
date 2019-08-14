using DefaultNamespace;
using StormiumTeam.Networking.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase.Components
{
	public struct DefaultHealthSnapshotData : ISnapshotFromComponent<DefaultHealthSnapshotData, DefaultHealthData>
	{
		public uint Tick { get; private set; }

		public uint OwnerGhostId;

		public int Value;
		public int Max;

		public void PredictDelta(uint tick, ref DefaultHealthSnapshotData baseline1, ref DefaultHealthSnapshotData baseline2)
		{
		}

		public void Interpolate(ref DefaultHealthSnapshotData target, float factor)
		{
			this = target;
		}

		public void Serialize(ref DefaultHealthSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			writer.WritePackedUInt(OwnerGhostId, compressionModel);
			writer.WritePackedInt(Value, compressionModel);
			writer.WritePackedInt(Max, compressionModel);
		}

		public void Deserialize(uint tick, ref DefaultHealthSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			OwnerGhostId = reader.ReadPackedUInt(ref ctx, compressionModel);
			Value        = reader.ReadPackedInt(ref ctx, compressionModel);
			Max          = reader.ReadPackedInt(ref ctx, compressionModel);
		}

		public void Set(DefaultHealthData component)
		{
			Value = component.Value;
			Max   = component.Max;
		}
	}
	
	public struct DefaultHealthGhostSerializer : IGhostSerializer<DefaultHealthSnapshotData>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<DefaultHealthSnapshotData>();

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			return 100;
		}

		public bool WantsPredictionDelta => false;

		public void BeginSerialize(ComponentSystemBase system)
		{
			system.GetGhostComponentType(out OwnerGhostType);
			system.GetGhostComponentType(out HealthDataGhostType);

			GhostSystemStateFromEntity = system.GetComponentDataFromEntity<GhostSystemStateComponent>();
		}

		public GhostComponentType<Owner>             OwnerGhostType;
		public GhostComponentType<DefaultHealthData> HealthDataGhostType;

		[NativeDisableContainerSafetyRestriction]
		public ComponentDataFromEntity<GhostSystemStateComponent> GhostSystemStateFromEntity;

		public bool CanSerialize(EntityArchetype arch)
		{
			var components = arch.GetComponentTypes();
			var count      = 0;
			for (var i = 0; i != components.Length; i++)
			{
				if (components[i] == OwnerGhostType) count++;
				if (components[i] == HealthDataGhostType) count++;
			}

			return count == 2;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref DefaultHealthSnapshotData snapshot)
		{
			var owner = chunk.GetNativeArray(OwnerGhostType.Archetype)[ent];
			snapshot.OwnerGhostId = GhostSystemStateFromEntity.GetGhostId(owner.Target);

			var healthData = chunk.GetNativeArray(HealthDataGhostType.Archetype)[ent];
			snapshot.Value = healthData.Value;
			snapshot.Max   = healthData.Max;
		}
	}
	
	public class DefaultHealthClientSpawnSystem : DefaultGhostSpawnSystem<DefaultHealthSnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				ComponentType.ReadWrite<DefaultHealthSnapshotData>(),
				ComponentType.ReadWrite<HealthDescription>(),
				ComponentType.ReadWrite<DefaultHealthData>(),
				ComponentType.ReadWrite<HealthConcreteValue>(),
				ComponentType.ReadWrite<Owner>(),
				ComponentType.ReadWrite<ReplicatedEntityComponent>()
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return GetGhostArchetype();
		}
	}
	
	public class DefaultHealthSynchronizeFromSnapshot : BaseUpdateFromSnapshotSystem<DefaultHealthSnapshotData, DefaultHealthData>
	{
		private struct SetOwner : IJobForEach_BC<DefaultHealthSnapshotData, Owner>
		{
			public            uint                       ServerTick;
			[ReadOnly] public NativeHashMap<int, Entity> GhostEntityMap;

			public void Execute(DynamicBuffer<DefaultHealthSnapshotData> data, ref Owner owner)
			{
				if (!data.GetDataAtTick(ServerTick, out var snapshot))
					return;

				GhostEntityMap.TryGetValue((int) snapshot.OwnerGhostId, out owner.Target);
			}
		}

		private ConvertGhostEntityMap m_ConvertGhostEntityMap;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_ConvertGhostEntityMap = World.GetOrCreateSystem<ConvertGhostEntityMap>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = base.OnUpdate(inputDeps);
			return new SetOwner
			{
				GhostEntityMap = m_ConvertGhostEntityMap.HashMap,
				ServerTick     = NetworkTimeSystem.interpolateTargetTick
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, m_ConvertGhostEntityMap.dependency));
		}
	}
}