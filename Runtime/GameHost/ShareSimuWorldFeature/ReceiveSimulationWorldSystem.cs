using System;
using System.Collections.Generic;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.GameHost.Simulation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;

namespace DefaultNamespace
{
	public struct ComponentTypeDetails
	{
		public int Size;
		public string Name;
	}
	
	public class ReceiveSimulationWorldSystem : SystemBase
	{
		private RegisterDeserializerSystem          registerDeserializer;
		private NativeHashMap<GhGameEntity, Entity> ghToUnityEntityMap;

		public Dictionary<GhComponentType, ComponentTypeDetails> typeDetailMap;
		public Dictionary<uint, NativeArray<uint>> archetypeMap;

		private EntityArchetype defaultSpawnArchetype;

		protected override void OnCreate()
		{
			base.OnCreate();

			registerDeserializer = World.GetOrCreateSystem<RegisterDeserializerSystem>();
			ghToUnityEntityMap   = new NativeHashMap<GhGameEntity, Entity>(64, Allocator.Persistent);
			
			typeDetailMap = new Dictionary<GhComponentType, ComponentTypeDetails>();
			archetypeMap = new Dictionary<uint, NativeArray<uint>>();

			defaultSpawnArchetype = EntityManager.CreateArchetype(typeof(ReplicatedGameEntity));
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
			/* flow map:
			 * 1. Component Type
			 * 2. Archetype
			 * 3. Entity
			 * 4. Component
			 */

			// ---- 1. Component Type
			//
			using var componentTypes = new NativeArray<GhComponentType>(reader.ReadValue<int>(), Allocator.Temp);
			if (componentTypes.Length > 0)
			{
				reader.ReadDataSafe((byte*) componentTypes.GetUnsafePtr(), sizeof(GhComponentType) * componentTypes.Length);
				
				// 1.5 Read description of component type
				foreach (var componentType in componentTypes)
				{
					ComponentTypeDetails details;
					details.Size = reader.ReadValue<int>();
					details.Name = reader.ReadString();
					
					typeDetailMap[componentType] = details;
				}
			}
			else
			{
				return;
			}

			// ---- 2. Archetype
			//
			using var archetypes = new NativeArray<uint>(reader.ReadValue<int>(), Allocator.Temp);
			if (archetypes.Length > 0)
			{
				reader.ReadDataSafe(archetypes);
				
				// 2.1 read registered component of each archetype
				for (var i = 0; i != archetypes.Length; i++)
				{
					var row    = archetypes[i];
					var length = reader.ReadValue<int>();
					if (!archetypeMap.TryGetValue(row, out var archetypeTypeArray) || archetypeTypeArray.Length != length)
					{
						if (archetypeTypeArray.IsCreated)
							archetypeTypeArray.Dispose();
						archetypeTypeArray = new NativeArray<uint>(length, Allocator.Persistent);
						archetypeMap[row]  = archetypeTypeArray;
					}

					reader.ReadDataSafe(archetypeTypeArray);
				}
			}
			else
			{
				return;
			}

			// ---- 3. Entity
			//
			using var entities = new NativeArray<GhGameEntity>(reader.ReadValue<int>(), Allocator.Temp);
			if (entities.Length > 0)
			{
				// 3.1
				reader.ReadDataSafe(entities);
				
				// 3.2 archetypes
				var entitiesArchetype = new NativeArray<uint>(reader.ReadValue<int>(), Allocator.Temp);
				reader.ReadDataSafe(entitiesArchetype);
				
				foreach (var entity in entities)
				{
					if (!ghToUnityEntityMap.TryGetValue(entity, out _))
					{
						ghToUnityEntityMap[entity] = EntityManager.CreateEntity(defaultSpawnArchetype);
						EntityManager.SetComponentData(ghToUnityEntityMap[entity], new ReplicatedGameEntity
						{
							Source = entity,
							ArchetypeId = entitiesArchetype[(int) entity.Id]
						});

						Debug.Log($"Created Unity Entity ({ghToUnityEntityMap[entity]}) for GameHost Entity (Row={entity.Id}, Arch={entitiesArchetype[(int) entity.Id]})");
					}
				}
			}
			else
			{
				ghToUnityEntityMap.Clear();
				return;
			}

			// ---- 4. Component
			//
			// 4.1 Transforming gh entities to unity entities
			/*var outputEntities = new NativeArray<Entity>(entities.Length, Allocator.Temp);
			for (var i = 0; i != entities.Length; i++)
				outputEntities[i] = ghToUnityEntityMap[entities[i]];

			// 4.2 Deserialize Components
			for (var i = 0; i < componentTypes.Length; i++)
			{
				var componentTypeBuffer = new DataBufferReader(reader, reader.CurrReadIndex, reader.Length);
				OnReadComponent(ref componentTypeBuffer, i, entities, outputEntities, componentTypes);

				reader.CurrReadIndex += componentTypeBuffer.CurrReadIndex;
			}

			outputEntities.Dispose();*/
		}

		/*private unsafe void OnReadComponent(ref DataBufferReader reader, int index, NativeArray<GhGameEntity> entities, NativeArray<Entity> output, NativeArray<GhComponentType> componentTypes)
		{
			var skip = reader.ReadValue<int>();
			var deserializer = registerDeserializer.GetDeserializer(size, name);
			if (deserializer == null)
			{
				Debug.LogWarning("Serializer not found!");

				reader.CurrReadIndex = skip;
				return;
			}

			deserializer.BeginDeserialize(this);
			deserializer.Deserialize(EntityManager, entities, output, ref reader);
		}*/
	}
}