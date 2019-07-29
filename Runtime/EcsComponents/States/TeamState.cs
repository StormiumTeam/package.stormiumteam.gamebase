using StormiumTeam.GameBase;
using StormiumTeam.Networking.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(Relative<TeamDescription>))]
[assembly: RegisterGenericComponentType(typeof(GhostRelative<TeamDescription>))]

namespace StormiumTeam.GameBase
{
	public struct TeamDescription : IEntityDescription
	{
	}

	// added automatically
	[InternalBufferCapacity(0)]
	public struct TeamEntityContainer : IBufferElementData
	{
		public Entity Value;
	}

	public struct TeamEmptySnapshotData : ISnapshotData<TeamEmptySnapshotData>
	{
		public uint Tick { get; set; }

		public void PredictDelta(uint tick, ref TeamEmptySnapshotData baseline1, ref TeamEmptySnapshotData baseline2)
		{
		}

		public void Serialize(ref TeamEmptySnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
		}

		public void Deserialize(uint tick, ref TeamEmptySnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;
		}

		public void Interpolate(ref TeamEmptySnapshotData target, float factor)
		{
		}
	}

	public struct TeamEmptyGhostSerializer : IGhostSerializer<TeamEmptySnapshotData>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<TeamEmptySnapshotData>();

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			return 1;
		}

		public bool WantsPredictionDelta => false;

		public GhostComponentType<TeamDescription> TeamDescriptionGhostType;

		public void BeginSerialize(ComponentSystemBase system)
		{
			system.GetGhostComponentType(out TeamDescriptionGhostType);
		}

		public bool CanSerialize(EntityArchetype arch)
		{
			var matches = 0;
			var comps   = arch.GetComponentTypes();
			for (var i = 0; i != comps.Length; i++)
			{
				if (comps[i] == TeamDescriptionGhostType) matches++;
			}

			return matches == 1;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref TeamEmptySnapshotData snapshot)
		{
			snapshot.Tick = tick;
		}
	}

	public class TeamEmptyGhostSpawnSystem : DefaultGhostSpawnSystem<TeamEmptySnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(TeamDescription),
				typeof(TeamEmptySnapshotData),
				typeof(ReplicatedEntityComponent)
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(TeamDescription),
				typeof(TeamEmptySnapshotData),
				typeof(ReplicatedEntityComponent),
				typeof(PredictedEntityComponent)
			);
		}
	}

	public class TeamUpdateContainerSystem : JobComponentSystem
	{
		private struct JobClearBuffer : IJobForEach_B<TeamEntityContainer>
		{
			public void Execute(DynamicBuffer<TeamEntityContainer> ctn)
			{
				ctn.Clear();
			}
		}

		[BurstCompile]
		private struct JobFindAndAdd : IJobForEachWithEntity<Relative<TeamDescription>>
		{
			public BufferFromEntity<TeamEntityContainer> ContainerFromEntity;

			public void Execute(Entity entity, int index, ref Relative<TeamDescription> teamRelative)
			{
				if (teamRelative.Target == default || !ContainerFromEntity.Exists(teamRelative.Target))
					return;

				var ctn = default(TeamEntityContainer);
				ctn.Value = entity;

				ContainerFromEntity[teamRelative.Target].Add(ctn);
			}
		}

		private EntityQuery m_TeamWithoutContainer;
		private EntityQuery m_TeamWithContainer;
		private EntityQuery m_EntityWithTeam;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_TeamWithoutContainer = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(TeamDescription)},
				None = new ComponentType[] {typeof(TeamEntityContainer)}
			});
			m_TeamWithContainer = GetEntityQuery(typeof(TeamDescription), typeof(TeamEntityContainer));
			m_EntityWithTeam    = GetEntityQuery(typeof(Relative<TeamDescription>));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (m_TeamWithoutContainer.CalculateLength() > 0)
			{
				EntityManager.AddComponent(m_TeamWithoutContainer, typeof(TeamEntityContainer));
				var entities = m_TeamWithoutContainer.ToEntityArray(Allocator.TempJob);
				foreach (var ent in entities)
				{
					EntityManager.GetBuffer<TeamEntityContainer>(ent).Reserve(10);
				}
			}

			inputDeps = new JobClearBuffer().Schedule(m_TeamWithContainer, inputDeps);
			inputDeps = new JobFindAndAdd
			{
				ContainerFromEntity = GetBufferFromEntity<TeamEntityContainer>()
			}.ScheduleSingle(m_EntityWithTeam, inputDeps);

			return inputDeps;
		}
	}
}