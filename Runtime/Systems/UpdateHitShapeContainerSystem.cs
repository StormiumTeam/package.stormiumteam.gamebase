using Revolution.NetCode;
using StormiumTeam.GameBase.Data;
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
			Value = value;
			AttachedToParent = attachedToParent;
		}
	}

	public struct HitShapeFollowParentTag : IComponentData
	{
	}

	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	[UpdateAfter(typeof(DestroyChainReactionSystemClientServerWorld))]
	public class UpdateHitShapeContainerSystem : JobGameBaseSystem
	{
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

			[BurstDiscard]
			private void NonBurst_ErrorNoHitShapeContainer(Entity hitShape, Entity owner)
			{
				if (owner == default)
					return;
				Debug.LogError($"No HitShapeContainer found on owner={owner}, hitShape={hitShape}");
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


		private EntityQuery m_OwnerQuery;
		private EntityQuery m_DataQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_OwnerQuery = GetEntityQuery(typeof(HitShapeContainer));
			m_DataQuery  = GetEntityQuery(typeof(Owner), typeof(HitShapeDescription));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			m_OwnerQuery.AddDependency(inputDeps);

			inputDeps = new ClearBufferJob
			{
				Entities                    = m_OwnerQuery.ToEntityArray(Allocator.TempJob, out var dep1),
				HitShapeContainerFromEntity = GetBufferFromEntity<HitShapeContainer>()
			}.Schedule(JobHandle.CombineDependencies(inputDeps, dep1));

			inputDeps = new UpdateBufferJob
			{
				HitShapeContainerFromEntity = GetBufferFromEntity<HitShapeContainer>(),
				FollowTagFromEntity         = GetComponentDataFromEntity<HitShapeFollowParentTag>(true)
			}.ScheduleSingle(m_DataQuery, inputDeps);

			return inputDeps;
		}
	}
}