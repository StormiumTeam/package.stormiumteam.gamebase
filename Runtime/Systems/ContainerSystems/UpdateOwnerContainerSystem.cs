using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Systems
{
	public class UpdateOwnerContainerSystem : AbsGameBaseSystem
	{
		private EntityQuery                            m_DataQuery;
		private EndSimulationEntityCommandBufferSystem m_EndBarrier;


		private EntityQuery m_OwnerQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_OwnerQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(OwnerChild)}
			});
			m_DataQuery = GetEntityQuery(typeof(Owner));

			m_EndBarrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override void OnUpdate()
		{
			m_OwnerQuery.AddDependency(Dependency);

			Dependency = new ClearBufferJob
			{
				Entities             = m_OwnerQuery.ToEntityArrayAsync(Allocator.TempJob, out var dep1),
				OwnerChildFromEntity = GetBufferFromEntity<OwnerChild>()
			}.Schedule(JobHandle.CombineDependencies(Dependency, dep1));

			Dependency = new UpdateBufferJob
			{
				EntityDescriptionFromEntity = GetComponentDataFromEntity<EntityDescription>(true),
				OwnerChildFromEntity        = GetBufferFromEntity<OwnerChild>(),
				EntityCommandBuffer         = m_EndBarrier.CreateCommandBuffer(),
				OwnerChildType              = typeof(OwnerChild)
			}.ScheduleSingle(m_DataQuery, Dependency);

			m_EndBarrier.AddJobHandleForProducer(Dependency);
		}

		[BurstCompile]
		private struct ClearBufferJob : IJob
		{
			[DeallocateOnJobCompletion]
			public NativeArray<Entity> Entities;

			public BufferFromEntity<OwnerChild> OwnerChildFromEntity;

			public void Execute()
			{
				for (var i = 0; i != Entities.Length; i++)
				{
					OwnerChildFromEntity[Entities[i]].Clear();
					OwnerChildFromEntity[Entities[i]].Reserve(8);
				}
			}
		}

		[BurstCompile]
		private struct UpdateBufferJob : IJobForEachWithEntity<Owner>
		{
			[ReadOnly] public ComponentDataFromEntity<EntityDescription> EntityDescriptionFromEntity;

			public BufferFromEntity<OwnerChild> OwnerChildFromEntity;
			public EntityCommandBuffer          EntityCommandBuffer;
			public ComponentType                OwnerChildType;

			[BurstDiscard]
			private void NonBurst_ErrorNoOwnerChild(Entity action, Entity owner)
			{
				if (owner == default)
					return;
				//Debug.LogError($"No OwnerChild found on owner={owner}, action={action}");
			}

			public void Execute(Entity entity, int i, ref Owner owner)
			{
				if (owner.Target == default || !OwnerChildFromEntity.Exists(owner.Target))
				{
					NonBurst_ErrorNoOwnerChild(entity, owner.Target);
					if (owner.Target != default)
						EntityCommandBuffer.AddComponent(owner.Target, OwnerChildType);
					return;
				}

				var typeId = EntityDescriptionFromEntity.TryGet(entity, out var desc)
					? desc.Value.TypeIndex
					: 0;

				OwnerChildFromEntity[owner.Target].Add(new OwnerChild {TypeId = typeId, Child = entity});
			}
		}
	}
}