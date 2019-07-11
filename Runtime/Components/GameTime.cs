using StormiumTeam.Networking.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public struct GameTime
	{
		public int    Frame;
		public int    Tick;
		public int    DeltaTick;
		public int    FixedTickPerSecond;
		public double Time;
		public float  DeltaTime;
	}

	public struct GameTimeComponent : IComponentData
	{
		public GameTime Value;

		public int    Frame              => Value.Frame;
		public int    Tick               => Value.Tick;
		public int    DeltaTick          => Value.DeltaTick;
		public int    FixedTickPerSecond => Value.FixedTickPerSecond;
		public double Time               => Value.Time;
		public float  DeltaTime          => Value.DeltaTime;

		public GameTimeComponent(GameTime value)
		{
			Value = value;
		}
	}

	public struct SynchronizedSimulationTime : IComponentData
	{
		public uint Interpolated;
		public uint Predicted;

		public double InterpolatedReal => Interpolated * 0.001f;
		public double PredictedReal    => Predicted * 0.0001f;
	}

	public struct SynchronizedSimulationTimeSnapshot : ISnapshotData<SynchronizedSimulationTimeSnapshot>
	{
		public uint Tick { get; set; }

		public uint TimeInterpolated;
		public uint TimePredicted;

		public void PredictDelta(uint tick, ref SynchronizedSimulationTimeSnapshot baseline1, ref SynchronizedSimulationTimeSnapshot baseline2)
		{
			var predicted = new GhostDeltaPredictor(tick, this.Tick, baseline1.Tick, baseline2.Tick);
			TimePredicted = (uint) predicted.PredictInt((int) TimePredicted, (int) baseline1.TimePredicted, (int) baseline2.TimePredicted);
		}

		public void Serialize(ref SynchronizedSimulationTimeSnapshot baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			writer.WritePackedUIntDelta(TimeInterpolated, baseline.TimeInterpolated, compressionModel);
		}

		public void Deserialize(uint tick, ref SynchronizedSimulationTimeSnapshot baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			TimeInterpolated = reader.ReadPackedUIntDelta(ref ctx, baseline.TimeInterpolated, compressionModel);
			TimePredicted    = TimeInterpolated;
		}

		public void Interpolate(ref SynchronizedSimulationTimeSnapshot target, float factor)
		{
			TimeInterpolated = (uint) math.lerp(TimeInterpolated, target.TimeInterpolated, factor);
			TimePredicted    = (uint) math.lerp(TimePredicted, target.TimePredicted, factor);
		}
	}

	public struct SynchronizedSimulationTimeGhostSerializer : IGhostSerializer<SynchronizedSimulationTimeSnapshot>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<SynchronizedSimulationTimeSnapshot>();

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			return 1000; // time is very important
		}

		public bool WantsPredictionDelta => true;
		
		public GhostComponentType<SynchronizedSimulationTime> GhostSynchronizedSimulationTimeType;

		public void BeginSerialize(ComponentSystemBase system)
		{
			system.GetGhostComponentType(out GhostSynchronizedSimulationTimeType);
		}

		public bool CanSerialize(EntityArchetype arch)
		{
			var types = arch.GetComponentTypes();
			for (var i = 0; i != types.Length; i++)
			{
				if (types[i] == GhostSynchronizedSimulationTimeType)
					return true;
			}

			return false;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref SynchronizedSimulationTimeSnapshot snapshot)
		{
			var component = chunk.GetNativeArray(GhostSynchronizedSimulationTimeType.Archetype)[ent];

			snapshot.Tick             = tick;
			snapshot.TimeInterpolated = component.Interpolated;
			snapshot.TimePredicted    = component.Predicted;
		}
	}

	public class SynchronizedSimulationTimeGhostSpawnSystem : DefaultGhostSpawnSystem<SynchronizedSimulationTimeSnapshot>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(SynchronizedSimulationTime),
				typeof(SynchronizedSimulationTimeSnapshot),

				ComponentType.ReadWrite<ReplicatedEntityComponent>()
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(SynchronizedSimulationTime),
				typeof(SynchronizedSimulationTimeSnapshot),

				ComponentType.ReadWrite<ReplicatedEntityComponent>(),
				ComponentType.ReadWrite<PredictedEntityComponent>()
			);
		}
	}

	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	public class SynchronizedSimulationTimeSystem : JobComponentSystem
	{
		private NativeArray<SynchronizedSimulationTime> m_TimeArray;

		private SynchronizedSimulationTimeGhostUpdateSystem m_GhostUpdateSystem;
		//private SynchronizedSimulationTime* m_ArrayPtr;
		
		private JobHandle m_Handle;

		public SynchronizedSimulationTime Value
		{
			get
			{
				if (!m_Handle.IsCompleted)
					m_Handle.Complete();
				return m_TimeArray[0];
			}
		}

		private struct GetTimeJob : IJobForEachWithEntity<SynchronizedSimulationTime>
		{
			[NativeDisableContainerSafetyRestriction]
			public NativeArray<SynchronizedSimulationTime> TimeArray;

			public void Execute(Entity entity, int index, ref SynchronizedSimulationTime time)
			{
				TimeArray[0] = time;
			}
		}

		private EntityQuery m_TimeQuery;

		public JobHandle InputDependency;
		public JobHandle OutputDependency;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_TimeQuery = GetEntityQuery(typeof(SynchronizedSimulationTime));
			m_TimeArray = new NativeArray<SynchronizedSimulationTime>(1, Allocator.Persistent);

			Enabled = false;
		}

		protected override void OnStartRunning()
		{
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			m_TimeArray.Dispose();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			m_Handle.Complete();
			m_TimeQuery.CompleteDependency();
			
			return OutputDependency = m_Handle = new GetTimeJob
			{
				TimeArray = m_TimeArray
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, InputDependency));
		}

		public JobHandle Schedule(NativeArray<SynchronizedSimulationTime> array, JobHandle inputDeps)
		{
			m_TimeQuery.CompleteDependency();
			return new GetTimeJob
			{
				TimeArray = array
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, m_Handle));		
		}
	}

	[UpdateInGroup(typeof(GhostUpdateSystemGroup))]
	public class SynchronizedSimulationTimeGhostUpdateSystem : JobComponentSystem
	{
		public JobHandle LastHandle;
		
		[BurstCompile]
		[RequireComponentTag(typeof(SynchronizedSimulationTimeSnapshot))]
		private struct UpdateInterpolatedJob : IJobForEachWithEntity<SynchronizedSimulationTime>
		{
			[NativeDisableParallelForRestriction] public BufferFromEntity<SynchronizedSimulationTimeSnapshot> snapshotFromEntity;
			public                                       uint                                                 interpolateTargetTick;
			public                                       uint                                                 predictTargetTick;

			public void Execute(Entity entity, int index, ref SynchronizedSimulationTime ghostTime)
			{
				var snapshot = snapshotFromEntity[entity];
				snapshot.GetDataAtTick(interpolateTargetTick, out var interpolatedSnapshot);
				snapshot.GetDataAtTick(predictTargetTick, out var predictSnapshot);

				ghostTime.Interpolated = interpolatedSnapshot.TimeInterpolated;
				ghostTime.Predicted    = predictSnapshot.TimePredicted;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var updateInterpolatedJob = new UpdateInterpolatedJob
			{
				snapshotFromEntity    = GetBufferFromEntity<SynchronizedSimulationTimeSnapshot>(),
				interpolateTargetTick = NetworkTimeSystem.interpolateTargetTick,
				predictTargetTick     = NetworkTimeSystem.predictTargetTick
			};
			inputDeps = updateInterpolatedJob.Schedule(this, inputDeps);
			var system = World.GetExistingSystem<SynchronizedSimulationTimeSystem>();
			system.InputDependency = inputDeps;
			system.Enabled = true;
			system.Update();
			system.Enabled = false;

			return system.OutputDependency;
		}
	}
}