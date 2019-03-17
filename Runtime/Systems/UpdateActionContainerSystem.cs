﻿using package.StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public class UpdateActionContainerSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			// First clear buffers...
			ForEach((DynamicBuffer<ActionContainer> buffer) => { buffer.Clear(); });

			ForEach((Entity entity, ref ActionTag actionTag, ref OwnerState<LivableDescription> livable) =>
			{
				if (!EntityManager.Exists(livable.Target))
				{
					Debug.LogError("Owner doesn't exist anymore.");
					return;
				}

				var newBuffer = EntityManager.HasComponent(livable.Target, typeof(ActionContainer))
					? PostUpdateCommands.SetBuffer<ActionContainer>(livable.Target)
					: PostUpdateCommands.AddBuffer<ActionContainer>(livable.Target);

				newBuffer.Add(new ActionContainer(entity));
			});
		}
	}
}