using System;
using package.stormiumteam.shared.ecs;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.GameHost.Simulation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace
{
	public class DefaultSingleDeserializer<TComponent> : ICustomComponentDeserializer
		where TComponent : struct, IComponentData
	{
		public int Size => UnsafeUtility.SizeOf<TComponent>();

		public void BeginDeserialize(SystemBase system)
		{
		}

		public void Deserialize(EntityManager entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, ref DataBufferReader reader)
		{
			var links = new NativeArray<GhComponentMetadata>(reader.ReadValue<int>(), Allocator.Temp);
			reader.ReadDataSafe(links);

			var components = new NativeArray<TComponent>(reader.ReadValue<int>(), Allocator.Temp);
			var comp = 0;
			reader.ReadDataSafe(components);
			for (var ent = 0; ent < gameEntities.Length; ent++)
			{
				var entity = gameEntities[ent];
				if (links[(int) entity.Id].Null)
					continue;
				
				entityManager.SetOrAddComponentData(output[ent], components[comp++]);
			}
		}
	}
}