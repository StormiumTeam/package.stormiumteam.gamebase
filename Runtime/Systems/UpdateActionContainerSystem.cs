using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace StormiumTeam.GameBase
{
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
					buffer.Reserve(buffer.Capacity + 1);
				})
				.Run();

			var bufferFromEntity = GetBufferFromEntity<ActionContainer>();
			inputDeps = Entities
			            .WithAll<ActionDescription>()
			            .ForEach((Entity entity, in Owner owner) =>
			            {
				            if (owner.Target == Entity.Null || !bufferFromEntity.Exists(owner.Target))
					            return;

				            bufferFromEntity[owner.Target].Add(new ActionContainer(entity));
			            })
			            .Schedule(inputDeps);

			return inputDeps;
		}
	}
}