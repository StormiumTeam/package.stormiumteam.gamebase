using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public struct HitShapeContainer : IBufferElementData
	{
		public Entity Value;
		public bool   AttachedToParent;

		public HitShapeContainer(Entity value, bool attachedToParent = false)
		{
			Value            = value;
			AttachedToParent = attachedToParent;
		}
	}

	public struct HitShapeFollowParentTag : IComponentData
	{
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.ConfigureSpawnedEntities))]
	public class UpdateHitShapeContainerSystem : AbsGameBaseSystem
	{
		private EntityQuery m_DataQuery;


		private EntityQuery m_OwnerQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_OwnerQuery = GetEntityQuery(typeof(HitShapeContainer));
			m_DataQuery  = GetEntityQuery(typeof(Owner), typeof(HitShapeDescription));
		}

		protected override void OnUpdate()
		{
			m_OwnerQuery.AddDependency(Dependency);

			Dependency = new ClearBufferJob
			{
				Entities                    = m_OwnerQuery.ToEntityArrayAsync(Allocator.TempJob, out var dep1),
				HitShapeContainerFromEntity = GetBufferFromEntity<HitShapeContainer>()
			}.Schedule(JobHandle.CombineDependencies(Dependency, dep1));

			Dependency = new UpdateBufferJob
			{
				HitShapeContainerFromEntity = GetBufferFromEntity<HitShapeContainer>(),
				FollowTagFromEntity         = GetComponentDataFromEntity<HitShapeFollowParentTag>(true),
				IsServer = IsServer
			}.ScheduleSingle(m_DataQuery, Dependency);
		}

		[BurstCompile]
		private struct ClearBufferJob : IJob
		{
			[DeallocateOnJobCompletion]
			public NativeArray<Entity> Entities;

			public BufferFromEntity<HitShapeContainer> HitShapeContainerFromEntity;

			public void Execute()
			{
				for (var i = 0; i != Entities.Length; i++)
				{
					HitShapeContainerFromEntity[Entities[i]].Clear();
					HitShapeContainerFromEntity[Entities[i]].Reserve(8);
				}
			}
		}

		[BurstCompile]
		private struct UpdateBufferJob : IJobForEachWithEntity<Owner>
		{
			public            BufferFromEntity<HitShapeContainer>              HitShapeContainerFromEntity;
			[ReadOnly] public ComponentDataFromEntity<HitShapeFollowParentTag> FollowTagFromEntity;
			public bool IsServer;

			[BurstDiscard]
			private void NonBurst_ErrorNoHitShapeContainer(Entity hitShape, Entity owner)
			{
				if (owner == default)
					return;
				Debug.LogError($"{IsServer} No HitShapeContainer found on owner={owner}, hitShape={hitShape}");
			}

			public void Execute(Entity entity, int i, ref Owner owner)
			{
				if (owner.Target == default || !HitShapeContainerFromEntity.Exists(owner.Target))
				{
					NonBurst_ErrorNoHitShapeContainer(entity, owner.Target);
					return;
				}

				HitShapeContainerFromEntity[owner.Target].Add(new HitShapeContainer(entity)
				{
					AttachedToParent = FollowTagFromEntity.Exists(entity)
				});
			}
		}
	}
}