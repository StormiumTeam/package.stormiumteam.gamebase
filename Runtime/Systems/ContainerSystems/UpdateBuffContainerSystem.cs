﻿using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;

namespace StormiumTeam.GameBase
{
	[AlwaysSynchronizeSystem]
	[UpdateInGroup(typeof(OrderGroup.Simulation.ConfigureSpawnedEntities))]
	public class UpdateBuffContainerSystem : AbsGameBaseSystem
	{
		private EndSimulationEntityCommandBufferSystem m_EndBarrier;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_EndBarrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override void OnUpdate()
		{
			Entities
				.WithStructuralChanges()
				.ForEach((Entity entity, ref DynamicBuffer<BuffContainer> buffer) =>
				{
					buffer.Clear();
					buffer.Capacity = 8;
				})
				.Run();

			var bufferFromEntity = GetBufferFromEntity<BuffContainer>();
			var ecb              = m_EndBarrier.CreateCommandBuffer();
			var componentType    = ComponentType.ReadWrite<BuffContainer>();

			Entities
				.WithAll<BuffModifierDescription>()
				.WithNone<BuffForTarget>()
				.ForEach((Entity entity, in Owner owner) =>
				{
					if (owner.Target == Entity.Null || !bufferFromEntity.Exists(owner.Target))
					{
						if (owner.Target != Entity.Null)
							ecb.AddComponent(owner.Target, componentType);
						return;
					}

					bufferFromEntity[owner.Target].Add(new BuffContainer(entity));
				})
				.Run();

			Entities
				.WithAll<BuffModifierDescription>()
				.ForEach((Entity entity, ref BuffForTarget target) =>
				{
					if (target.Target == Entity.Null || !bufferFromEntity.Exists(target.Target))
					{
						if (target.Target != Entity.Null)
							ecb.AddComponent(target.Target, componentType);
						return;
					}

					bufferFromEntity[target.Target].Add(new BuffContainer(entity));
				})
				.Run();
		}
	}
}