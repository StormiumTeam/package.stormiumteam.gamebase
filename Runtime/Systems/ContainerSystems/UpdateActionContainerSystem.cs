using Unity.Entities;
using Unity.Jobs;

namespace StormiumTeam.GameBase
{
	[AlwaysSynchronizeSystem]
	[UpdateInGroup(typeof(OrderGroup.Simulation.ConfigureSpawnedEntities))]
	public class UpdateActionContainerSystem : JobGameBaseSystem
	{
		private EndSimulationEntityCommandBufferSystem m_EndBarrier;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_EndBarrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			Entities
				.WithStructuralChanges()
				.ForEach((Entity entity, ref DynamicBuffer<ActionContainer> buffer) =>
				{
					buffer.Clear();
					buffer.Reserve(8);
				})
				.Run();

			var bufferFromEntity = GetBufferFromEntity<ActionContainer>();
			var ecb = m_EndBarrier.CreateCommandBuffer();
			var componentType = ComponentType.ReadWrite<ActionContainer>();
			Entities
				.WithAll<ActionDescription>()
				.ForEach((Entity entity, in Owner owner) =>
				{
					if (owner.Target == Entity.Null || !bufferFromEntity.Exists(owner.Target))
					{
						if (owner.Target != Entity.Null)
							ecb.AddComponent(owner.Target, componentType);
						return;
					}

					bufferFromEntity[owner.Target].Add(new ActionContainer(entity));
				})
				.Run();

			return default;
		}
	}
}