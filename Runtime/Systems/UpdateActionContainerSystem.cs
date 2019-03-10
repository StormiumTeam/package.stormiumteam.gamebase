using package.stormium.core;
using Unity.Entities;
using UnityEngine;

namespace Stormium.Core
{
	[UpdateInGroup(typeof(STUpdateOrder.UO_FinalizeData))]
	public class UpdateActionContainerSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			// First clear buffers...
			ForEach((DynamicBuffer<StActionContainer> buffer) => { buffer.Clear(); });

			ForEach((Entity entity, ref StActionTag actionTag, ref OwnerState<LivableDescription> livable) =>
			{
				if (!EntityManager.Exists(livable.Target))
				{
					Debug.LogError("Owner doesn't exist anymore.");
					return;
				}

				var newBuffer = EntityManager.HasComponent(livable.Target, typeof(StActionContainer))
					? PostUpdateCommands.SetBuffer<StActionContainer>(livable.Target)
					: PostUpdateCommands.AddBuffer<StActionContainer>(livable.Target);

				newBuffer.Add(new StActionContainer(entity));
			});
		}
	}
}