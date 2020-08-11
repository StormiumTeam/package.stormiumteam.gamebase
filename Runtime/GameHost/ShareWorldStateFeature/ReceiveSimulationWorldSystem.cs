using System;
using System.Collections.Generic;
using GameHost.Native;
using K4os.Compression.LZ4;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using RevolutionSnapshot.Core.Buffers;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

namespace GameHost.ShareSimuWorldFeature
{
	public struct ComponentTypeDetails
	{
		public GhComponentType Row;
		public int             Size;
		public CharBuffer256   Name;
	}

	public class ReceiveSimulationWorldSystem : SystemBase
	{
		public Dictionary<uint, Archetype__> archetypeMap;

		private EntityArchetype                     defaultSpawnArchetype;
		public NativeHashMap<GhGameEntity, Entity> ghToUnityEntityMap;

		private RegisterDeserializerSystem                      registerDeserializer;
		
		public NativeHashMap<CharBuffer256, ComponentTypeDetails>   typeDetailMapFromName;
		public NativeHashMap<GhComponentType, ComponentTypeDetails> typeDetailMapFromRow;
		
		private Dictionary<GhComponentType, ICustomComponentDeserializer> componentTypeToDeserializer;

		protected override void OnCreate()
		{
			base.OnCreate();

			registerDeserializer = World.GetOrCreateSystem<RegisterDeserializerSystem>();
			ghToUnityEntityMap   = new NativeHashMap<GhGameEntity, Entity>(64, Allocator.Persistent);

			typeDetailMapFromRow  = new NativeHashMap<GhComponentType, ComponentTypeDetails>(64, Allocator.Persistent);
			typeDetailMapFromName = new NativeHashMap<CharBuffer256, ComponentTypeDetails>(64, Allocator.Persistent);
			archetypeMap          = new Dictionary<uint, Archetype__>();

			defaultSpawnArchetype = EntityManager.CreateArchetype(typeof(ReplicatedGameEntity));
			
			componentTypeToDeserializer = new Dictionary<GhComponentType, ICustomComponentDeserializer>();
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
			componentTypeToDeserializer.Clear();
		}

		public unsafe JobHandle OnNewMessage(ref DataBufferReader reader)
		{
			var       compressedSize     = reader.ReadValue<int>();
			var       uncompressedSize   = reader.ReadValue<int>();
			using var compressedMemory   = new NativeArray<byte>(compressedSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			using var uncompressedMemory = new NativeArray<byte>(uncompressedSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			reader.ReadDataSafe(compressedMemory);

			LZ4Codec.Decode((byte*) compressedMemory.GetUnsafePtr(), compressedSize,
				(byte*) uncompressedMemory.GetUnsafePtr(), uncompressedSize);

			var uncompressedReader = new DataBufferReader((byte*) uncompressedMemory.GetUnsafePtr(), uncompressedMemory.Length);
			return __onNewMessage(ref uncompressedReader);
		}

		[BurstCompile]
		public unsafe struct ReadComponentTypeAndArchetypeJob : IJob
		{
			public NativeHashMap<CharBuffer256, ComponentTypeDetails>   typeDetailMapFromName;
			public NativeHashMap<GhComponentType, ComponentTypeDetails> typeDetailMapFromRow;
			
			public NativeArray<GhComponentType> ComponentTypeOutput;
			
			public UnsafeAllocation<DataBufferReader> ReaderAlloc;

			public void Execute()
			{
				ref var reader = ref ReaderAlloc.AsRef();
				if (ComponentTypeOutput.Length > 0)
				{
					reader.ReadDataSafe((byte*) ComponentTypeOutput.GetUnsafePtr(), sizeof(GhComponentType) * ComponentTypeOutput.Length);

					// 1.5 Read description of component type
					ComponentTypeDetails details;
					for (var i = 0; i < ComponentTypeOutput.Length; i++)
					{
						var componentType = ComponentTypeOutput[i];
						details.Row  = componentType;
						details.Size = reader.ReadValue<int>();
						details.Name = reader.ReadBuffer<CharBuffer256>();
						
						typeDetailMapFromRow[componentType] = details;
						typeDetailMapFromName[details.Name] = details;
					}
				}
			}
		}

		public struct ArchetypeUpdate
		{
			public GhGameEntity GhGameEntity;
			public Entity       UnityEntity;
			public uint         NewArchetype;
		}
		
		[BurstCompile]
		public unsafe struct ReadEntitiesJob : IJob
		{
			public NativeArray<GhGameEntity>         entities;
			public NativeArray<uint>                 entitiesArchetype;
			public NativeHashMap<GhGameEntity, Entity> ghToUnityEntityMap;
			public EntityManager                     EntityManager;
			public EntityArchetype defaultSpawnArchetype;

			public NativeList<ArchetypeUpdate> archetypeUpdates;

			public void Execute()
			{
				for (var ent = 0; ent < entities.Length; ent++)
				{
					var entity          = entities[ent];
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
						archetypeUpdates.Add(new ArchetypeUpdate
						{
							GhGameEntity = entity,
							UnityEntity  = unityEntity,
							NewArchetype = archetype
						});
					}
				}
			}
		}

		private unsafe JobHandle __onNewMessage(ref DataBufferReader reader)
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
			new ReadComponentTypeAndArchetypeJob
			{
				typeDetailMapFromName = typeDetailMapFromName,
				typeDetailMapFromRow  = typeDetailMapFromRow,
				ComponentTypeOutput   = componentTypes,
				ReaderAlloc           = UnsafeAllocation.From(ref reader)
			}.Run();

			if (componentTypes.Length == 0)
				return default;

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
						Console.WriteLine("new archetype!");

						archetypeMap[row] = archetype;
					}
				}
			}
			else
			{
				return default;
			}

			// ---- 3. Entity
			//
			Profiler.BeginSample("ENtity");
			using var entities = new NativeArray<GhGameEntity>(reader.ReadValue<int>(), Allocator.Temp);
			if (entities.Length > 0)
			{
				// 3.1
				reader.ReadDataSafe(entities);

				// 3.2 archetypes
				var entitiesArchetype = new NativeArray<uint>(reader.ReadValue<int>(), Allocator.Temp);
				reader.ReadDataSafe(entitiesArchetype);

				using var archetypeUpdates = new NativeList<ArchetypeUpdate>(Allocator.Temp);

				EntityManager.CompleteAllJobs();
				new ReadEntitiesJob
				{
					entities              = entities,
					entitiesArchetype     = entitiesArchetype,
					ghToUnityEntityMap    = ghToUnityEntityMap,
					EntityManager         = EntityManager,
					defaultSpawnArchetype = defaultSpawnArchetype,
					archetypeUpdates      = archetypeUpdates
				}.Run();
				foreach (var update in archetypeUpdates)
				{
					var previousArchetype = 0u;
					if (EntityManager.TryGetComponentData(update.UnityEntity, out ReplicatedGameEntity previousReplicatedData))
						previousArchetype = previousReplicatedData.ArchetypeId;

					if (previousArchetype > 0)
						foreach (var attach in archetypeMap[previousArchetype].Attaches)
							attach.OnEntityRemoved(EntityManager, update.GhGameEntity, update.UnityEntity);

					EntityManager.SetComponentData(update.UnityEntity, new ReplicatedGameEntity
					{
						Source      = update.GhGameEntity,
						ArchetypeId = update.NewArchetype
					});

					//Console.WriteLine($"Created gamehost entity! {update.GhGameEntity.Id}, {update.NewArchetype}, {update.UnityEntity}");
					if (update.NewArchetype > 0)
					{
						foreach (var attach in archetypeMap[update.NewArchetype].Attaches)
							attach.OnEntityAdded(EntityManager, update.GhGameEntity, update.UnityEntity);
					}

#if UNITY_EDITOR
					EntityManager.SetName(update.UnityEntity, $"GameHost Entity #{update.GhGameEntity.Id} (Arch={update.NewArchetype})");
#endif
				}
			}
			else
			{
				ghToUnityEntityMap.Clear();
				return default;
			}

			Profiler.EndSample();

			// ---- 4. Component
			//
			// 4.1 Transforming gh entities to unity entities
			Profiler.BeginSample("Component");
			var outputEntities = new NativeArray<Entity>(entities.Length, Allocator.Temp);
			Profiler.BeginSample("4.15 Convert Gh to Unity");
			for (var i = 0; i != entities.Length; i++)
				outputEntities[i] = ghToUnityEntityMap[entities[i]];
			Profiler.EndSample();

			// 4.2 Deserialize Components
			var deps = new NativeList<JobHandle>(Allocator.Temp);
			for (var i = 0; i < componentTypes.Length; i++)
			{
				var skip                = reader.ReadValue<int>();
				var componentTypeBuffer = new DataBufferReader(reader, reader.CurrReadIndex, reader.Length);
				deps.Add(OnReadComponent(ref componentTypeBuffer, i, entities, outputEntities, componentTypes));

				reader.CurrReadIndex += skip - sizeof(int);
			}

			Profiler.BeginSample("ScheduleBatched");
			JobHandle.ScheduleBatchedJobs();
			Profiler.EndSample();
			Profiler.EndSample();

			outputEntities.Dispose();
			return JobHandle.CombineDependencies(deps);
		}

		private HashSet<(CharBuffer256 name, int size)> warningSet = new HashSet<(CharBuffer256 name, int size)>();

		private JobHandle OnReadComponent(ref DataBufferReader reader, int index, NativeArray<GhGameEntity> entities, NativeArray<Entity> output, NativeArray<GhComponentType> componentTypes)
		{
			Profiler.BeginSample("Get Deserializer");
			if (!componentTypeToDeserializer.TryGetValue(componentTypes[index], out var deserializer))
			{
				var componentDetails = typeDetailMapFromRow[componentTypes[index]];
				deserializer     = registerDeserializer.Get(componentDetails.Size, componentDetails.Name).deserializer;

				componentTypeToDeserializer[componentTypes[index]] = deserializer;
			}
			Profiler.EndSample();
			if (deserializer == null)
			{
				var componentDetails = typeDetailMapFromRow[componentTypes[index]];
				var key              = (componentDetails.Name, componentDetails.Size);
				if (!warningSet.Contains(key))
				{
					Debug.LogWarning($"Serializer not found for {componentDetails.Name} (size={componentDetails.Size}, row={componentDetails.Row})");
					warningSet.Add(key);
				}

				return default;
			}

			Profiler.BeginSample(deserializer.GetType().Name);
			deserializer.BeginDeserialize(this);
			var jobHandle = deserializer.Deserialize(EntityManager, entities, output, reader);
			Profiler.EndSample();

			return jobHandle;
		}

		public struct Archetype__
		{
			public NativeArray<uint>                     ComponentTypes;
			public List<ICustomComponentArchetypeAttach> Attaches;
		}
	}
}