using package.StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	[UpdateAfter(typeof(DestroyChainReactionSystemClientServerWorld))]
	[UpdateAfter(typeof(GhostUpdateSystemGroup))]
	public class UpdateActionContainerSystem : JobGameBaseSystem
	{
		[BurstCompile]
		private struct ClearBufferJob : IJob
		{
			[DeallocateOnJobCompletion]
			public NativeArray<Entity>               Entities;
			public BufferFromEntity<ActionContainer> ActionContainerFromEntity;

			public void Execute()
			{
				for (var i = 0; i != Entities.Length; i++)
				{
					ActionContainerFromEntity[Entities[i]].Clear();
					ActionContainerFromEntity[Entities[i]].Reserve(8);
				}
			}
		}

		[BurstCompile]
		private struct UpdateBufferJob : IJobForEachWithEntity<Owner>
		{
			public BufferFromEntity<ActionContainer> ActionContainerFromEntity;

			[BurstDiscard]
			private void NonBurst_ErrorNoActionContainer(Entity action, Entity owner)
			{
				if (owner == default)
					return;
				Debug.LogError($"No ActionContainer found on owner={owner}, action={action}");
			}

			public void Execute(Entity entity, int i, ref Owner owner)
			{
				if (owner.Target == default || !ActionContainerFromEntity.Exists(owner.Target))
				{
					NonBurst_ErrorNoActionContainer(entity, owner.Target);
					return;
				}

				ActionContainerFromEntity[owner.Target].Add(new ActionContainer(entity));
			}
		}


		private EntityQuery m_OwnerQuery;
		private EntityQuery m_DataQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_OwnerQuery = GetEntityQuery(typeof(ActionContainer));
			m_DataQuery  = GetEntityQuery(typeof(Owner), typeof(ActionDescription));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			m_OwnerQuery.AddDependency(inputDeps);

			inputDeps = new ClearBufferJob
			{
				Entities                  = m_OwnerQuery.ToEntityArray(Allocator.TempJob, out var dep1),
				ActionContainerFromEntity = GetBufferFromEntity<ActionContainer>()
			}.Schedule(JobHandle.CombineDependencies(inputDeps, dep1));

			inputDeps = new UpdateBufferJob
			{
				ActionContainerFromEntity = GetBufferFromEntity<ActionContainer>()
			}.ScheduleSingle(m_DataQuery, inputDeps);

			return inputDeps;
		}
	}
}