using System;
using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace GameHost.ShareSimuWorldFeature
{
	public struct ComponentTypeDetails
	{
		public GhComponentType Row;
		public int             Size;
		public string          Name;
	}

	public class ReceiveSimulationWorldSystem : SystemBase
	{
		public Dictionary<uint, Archetype__> archetypeMap;

		private EntityArchetype                     defaultSpawnArchetype;
		public NativeHashMap<GhGameEntity, Entity> ghToUnityEntityMap;

		private RegisterDeserializerSystem               registerDeserializer;
		public  Dictionary<string, ComponentTypeDetails> typeDetailMapFromName;

		public Dictionary<GhComponentType, ComponentTypeDetails> typeDetailMapFromRow;

		protected override void OnCreate()
		{
			base.OnCreate();

			registerDeserializer = World.GetOrCreateSystem<RegisterDeserializerSystem>();
			ghToUnityEntityMap   = new NativeHashMap<GhGameEntity, Entity>(64, Allocator.Persistent);

			typeDetailMapFromRow  = new Dictionary<GhComponentType, ComponentTypeDetails>();
			typeDetailMapFromName = new Dictionary<string, ComponentTypeDetails>();
			archetypeMap          = new Dictionary<uint, Archetype__>();

			defaultSpawnArchetype = EntityManager.CreateArchetype(typeof(ReplicatedGameEntity));
		}

		protected override void OnUpdate()
		{
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			OnDisconnected();
		}

		public void OnDisconnected()
		{
			using (var query = EntityManager.CreateEntityQuery(typeof(ReplicatedGameEntity)))
				EntityManager.DestroyEntity(query);
			
			ghToUnityEntityMap.Clear();
			typeDetailMapFromRow.Clear();
			typeDetailMapFromName.Clear();

			foreach (var archetypeData in archetypeMap.Values)
			{
				archetypeData.Attaches.Clear();
				archetypeData.ComponentTypes.Dispose();
			}
			archetypeMap.Clear();
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
					details.Row  = componentType;
					details.Size = reader.ReadValue<int>();
					details.Name = reader.ReadString();

					typeDetailMapFromRow[componentType] = details;
					typeDetailMapFromName[details.Name] = details;
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
					var row     = archetypes[i];
					var length  = reader.ReadValue<int>();
					var newData = new NativeArray<uint>(length, Allocator.Temp);
					reader.ReadDataSafe(newData);

					if (!archetypeMap.TryGetValue(row, out var archetype)
					    || archetype.ComponentTypes.Length != length
					    || UnsafeUtility.MemCmp(archetype.ComponentTypes.GetUnsafePtr(), newData.GetUnsafePtr(), sizeof(uint) * length) != 0)
					{
						if (archetype.ComponentTypes.IsCreated)
							archetype.ComponentTypes.Dispose();
						archetype.ComponentTypes = new NativeArray<uint>(newData, Allocator.Persistent);
						archetype.Attaches       = new List<ICustomComponentArchetypeAttach>();
						registerDeserializer.AttachArchetype(ref archetype, typeDetailMapFromName);

						archetypeMap[row] = archetype;
					}
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
					var archetype       = entitiesArchetype[(int) entity.Id];
					var archetypeUpdate = false;
					if (!ghToUnityEntityMap.TryGetValue(entity, out var unityEntity))
					{
						ghToUnityEntityMap[entity] = unityEntity = EntityManager.CreateEntity(defaultSpawnArchetype);
						EntityManager.SetComponentData(unityEntity, new ReplicatedGameEntity
						{
							Source      = entity,
							ArchetypeId = archetype
						});
						archetypeUpdate = true;
					}
					else
					{
						if (EntityManager.GetComponentData<ReplicatedGameEntity>(unityEntity).ArchetypeId != archetype) 
							archetypeUpdate = true;
					}

					if (archetypeUpdate)
					{
						var previousArchetype = 0u;
						if (EntityManager.TryGetComponentData(unityEntity, out ReplicatedGameEntity previousReplicatedData))
							previousArchetype = previousReplicatedData.ArchetypeId;

						if (previousArchetype > 0)
							foreach (var attach in archetypeMap[previousArchetype].Attaches)
								attach.OnEntityRemoved(EntityManager, entity, unityEntity);

						EntityManager.SetComponentData(unityEntity, new ReplicatedGameEntity
						{
							Source      = entity,
							ArchetypeId = archetype
						});

						Console.WriteLine($"Created gamehost entity! {entity.Id}, {archetype}, {unityEntity}");
						foreach (var attach in archetypeMap[archetype].Attaches)
							attach.OnEntityAdded(EntityManager, entity, unityEntity);

#if UNITY_EDITOR
						EntityManager.SetName(unityEntity, $"GameHost Entity #{entity.Id} (Arch={archetype})");
#endif
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
			var outputEntities = new NativeArray<Entity>(entities.Length, Allocator.Temp);
			for (var i = 0; i != entities.Length; i++)
				outputEntities[i] = ghToUnityEntityMap[entities[i]];

			// 4.2 Deserialize Components
			for (var i = 0; i < componentTypes.Length; i++)
			{
				var componentTypeBuffer = new DataBufferReader(reader, reader.CurrReadIndex, reader.Length);
				OnReadComponent(ref componentTypeBuffer, i, entities, outputEntities, componentTypes);

				reader.CurrReadIndex += componentTypeBuffer.CurrReadIndex;
			}

			outputEntities.Dispose();
		}

		private void OnReadComponent(ref DataBufferReader reader, int index, NativeArray<GhGameEntity> entities, NativeArray<Entity> output, NativeArray<GhComponentType> componentTypes)
		{
			var skip             = reader.ReadValue<int>();
			var componentDetails = typeDetailMapFromRow[componentTypes[index]];
			var deserializer     = registerDeserializer.Get(componentDetails.Size, componentDetails.Name).deserializer;
			if (deserializer == null)
			{
				Debug.LogWarning($"Serializer not found for {componentDetails.Name} (size={componentDetails.Size}, row={componentDetails.Row})");

				reader.CurrReadIndex += skip;
				return;
			}

			deserializer.BeginDeserialize(this);
			deserializer.Deserialize(EntityManager, entities, output, ref reader);
		}

		public struct Archetype__
		{
			public NativeArray<uint>                     ComponentTypes;
			public List<ICustomComponentArchetypeAttach> Attaches;
		}
	}
}