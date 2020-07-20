using System;
using System.Collections.Generic;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.GameHost.Simulation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace
{
	public class ReceiveSimulationWorldSystem : SystemBase
	{
		private RegisterDeserializerSystem          registerDeserializer;
		private NativeHashMap<GhGameEntity, Entity> ghToUnityEntityMap;

		private EntityArchetype defaultSpawnArchetype;

		protected override void OnCreate()
		{
			base.OnCreate();

			registerDeserializer = World.GetOrCreateSystem<RegisterDeserializerSystem>();
			ghToUnityEntityMap   = new NativeHashMap<GhGameEntity, Entity>(64, Allocator.Persistent);

			defaultSpawnArchetype = EntityManager.CreateArchetype(typeof(GhGameEntity));
		}

		protected override void OnUpdate()
		{
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			ghToUnityEntityMap.Dispose();
		}

		public unsafe void OnNewMessage(ref DataBufferReader reader)
		{
			using var entities = new NativeArray<GhGameEntity>(reader.ReadValue<int>(), Allocator.Temp);
			if (entities.Length > 0)
			{
				reader.ReadDataSafe((byte*) entities.GetUnsafePtr(), sizeof(GhGameEntity) * entities.Length);
				foreach (var entity in entities)
				{
					if (!ghToUnityEntityMap.TryGetValue(entity, out _))
					{
						ghToUnityEntityMap[entity] = EntityManager.CreateEntity(defaultSpawnArchetype);
						EntityManager.SetComponentData(ghToUnityEntityMap[entity], entity);

						Console.WriteLine($"Created Unity Entity ({ghToUnityEntityMap[entity]}) for GameHost Entity (Row={entity.Id})");
					}
				}
			}
			else
			{
				ghToUnityEntityMap.Clear();
				return;
			}

			var outputEntities = new NativeArray<Entity>(entities.Length, Allocator.Temp);

			using var componentTypes = new NativeArray<GhComponentType>(reader.ReadValue<int>(), Allocator.Temp);
			if (componentTypes.Length > 0)
			{
				reader.ReadDataSafe((byte*) componentTypes.GetUnsafePtr(), sizeof(GhComponentType) * componentTypes.Length);

				for (var i = 0; i != entities.Length; i++)
					outputEntities[i] = ghToUnityEntityMap[entities[i]];

				for (var i = 0; i < componentTypes.Length; i++)
				{
					var componentTypeBuffer = new DataBufferReader(reader, reader.CurrReadIndex, reader.Length);
					OnReadComponent(ref componentTypeBuffer, i, entities, outputEntities, componentTypes);

					reader.CurrReadIndex += componentTypeBuffer.CurrReadIndex;
				}
			}
			else
			{
				outputEntities.Dispose();
				return;
			}

			outputEntities.Dispose();
		}

		private unsafe void OnReadComponent(ref DataBufferReader reader, int index, NativeArray<GhGameEntity> entities, NativeArray<Entity> output, NativeArray<GhComponentType> componentTypes)
		{
			var skip = reader.ReadValue<int>();
			var size = reader.ReadValue<int>();
			var name = reader.ReadString();

			var deserializer = registerDeserializer.GetDeserializer(size, name);
			if (deserializer == null)
			{
				Debug.LogWarning("Serializer not found!");
				
				reader.CurrReadIndex = skip;
				return;
			}
	
			deserializer.BeginDeserialize(this);
			deserializer.Deserialize(EntityManager, entities, output, ref reader);
		}
	}
}