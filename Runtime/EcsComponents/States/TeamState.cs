using Revolution;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[assembly: RegisterGenericComponentType(typeof(Relative<TeamDescription>))]

namespace StormiumTeam.GameBase
{
	public struct TeamDescription : IEntityDescription
	{
		public class Sync : RelativeSynchronize<TeamDescription>
		{
		}
	}

	// added automatically
	[InternalBufferCapacity(0)]
	public struct TeamEntityContainer : IBufferElementData
	{
		public Entity Value;
	}
	
	public class TeamUpdateContainerSystem : JobComponentSystem
	{
		private EntityQuery m_EntityWithTeam;
		private EntityQuery m_TeamWithContainer;

		private EntityQuery m_TeamWithoutContainer; 

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
			if (m_TeamWithoutContainer.CalculateEntityCount() > 0)
			{
				EntityManager.AddComponent(m_TeamWithoutContainer, typeof(TeamEntityContainer));
				var entities = m_TeamWithoutContainer.ToEntityArray(Allocator.TempJob);
				foreach (var ent in entities)
				{
					var teamEntityContainers = EntityManager.GetBuffer<TeamEntityContainer>(ent);
					teamEntityContainers.Capacity = (10);
				}
			}

			inputDeps = new JobClearBuffer().Schedule(m_TeamWithContainer, inputDeps);
			inputDeps = new JobFindAndAdd
			{
				ContainerFromEntity = GetBufferFromEntity<TeamEntityContainer>()
			}.ScheduleSingle(m_EntityWithTeam, inputDeps);

			return inputDeps;
		}

		[BurstCompile]
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

			public void Execute(Entity entity, int index, [ReadOnly] ref Relative<TeamDescription> teamRelative)
			{
				if (teamRelative.Target == default || !ContainerFromEntity.Exists(teamRelative.Target))
					return;

				var ctn = default(TeamEntityContainer);
				ctn.Value = entity;

				ContainerFromEntity[teamRelative.Target].Add(ctn);
			}
		}
	}
}