using Unity.Entities;
using Unity.Jobs;

namespace StormiumTeam.GameBase
{
	[AlwaysSynchronizeSystem]
	[UpdateInGroup(typeof(OrderGroup.Simulation.ConfigureSpawnedEntities))]
	public class UpdateActionContainerSystem : JobGameBaseSystem
	{
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
			Entities
				.WithAll<ActionDescription>()
				.ForEach((Entity entity, in Owner owner) =>
				{
					if (owner.Target == Entity.Null || !bufferFromEntity.Exists(owner.Target))
						return;

					bufferFromEntity[owner.Target].Add(new ActionContainer(entity));
				})
				.Run();

			return default;
		}
	}
}