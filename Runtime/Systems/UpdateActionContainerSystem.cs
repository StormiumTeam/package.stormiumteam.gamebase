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
			Entities.ForEach((DynamicBuffer<ActionContainer> buffer) => { buffer.Clear(); });

			Entities.WithAll<ActionDescription>().ForEach((Entity entity, ref Owner owner) =>
			{
				if (!EntityManager.Exists(owner.Target))
				{
					Debug.LogError("Owner doesn't exist anymore.");
					return;
				}

				var newBuffer = EntityManager.HasComponent(owner.Target, typeof(ActionContainer))
					? PostUpdateCommands.SetBuffer<ActionContainer>(owner.Target)
					: PostUpdateCommands.AddBuffer<ActionContainer>(owner.Target);

				newBuffer.Add(new ActionContainer(entity));
			});
		}
	}
}