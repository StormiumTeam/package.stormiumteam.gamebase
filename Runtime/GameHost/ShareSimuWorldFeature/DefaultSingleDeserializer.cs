﻿using System;
using System.Collections.Generic;
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

		private ComponentDataFromEntity<TComponent> componentDataFromEntity;

		public void BeginDeserialize(SystemBase system)
		{
			componentDataFromEntity = system.GetComponentDataFromEntity<TComponent>();
		}

		public void Deserialize(EntityManager entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, ref DataBufferReader reader)
		{
			var links = new NativeArray<GhComponentMetadata>(reader.ReadValue<int>(), Allocator.Temp);
			reader.ReadDataSafe(links);

			var components = new NativeArray<TComponent>(reader.ReadValue<int>(), Allocator.Temp);
			var comp       = 0;
			reader.ReadDataSafe(components);
			for (var ent = 0; ent < gameEntities.Length; ent++)
			{
				var entity = gameEntities[ent];
				if (links[(int) entity.Id].Null)
					continue;

				componentDataFromEntity[output[ent]] = components[comp++];
			}
		}
	}

	public class DefaultArchetypeAttach<TComponent> : ICustomComponentArchetypeAttach
		where TComponent : struct, IComponentData
	{
		public readonly string GameHostType;

		public DefaultArchetypeAttach(string ghType)
		{
			GameHostType = ghType;
		}

		public string[] RegisterTypes()
		{
			return new[] {GameHostType};
		}

		public bool CanAttachToArchetype(NativeArray<GhComponentType> componentTypes, Dictionary<string, ComponentTypeDetails> detailMap)
		{
			if (!detailMap.TryGetValue(GameHostType, out var details))
				return false;

			foreach (var component in componentTypes)
				if (component.Equals(details.Row))
					return true;

			return false;
		}

		public void OnEntityAdded(EntityManager entityManager, GhGameEntity ghEntity, Entity output)
		{
			entityManager.SetOrAddComponentData(output, new TComponent());
		}

		public void OnEntityRemoved(EntityManager entityManager, GhGameEntity ghEntity, Entity output)
		{
			if (entityManager.HasComponent<TComponent>(output))
				entityManager.RemoveComponent<TComponent>(output);
		}
	}
}