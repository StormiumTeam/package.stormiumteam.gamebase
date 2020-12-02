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

	[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
	public class ReceiveSimulationWorldSystem : SystemBase
	{
		public Dictionary<uint, Archetype__> archetypeMap;

		private EntityArchetype                                     defaultSpawnArchetype;
		public  NativeHashMap<GhGameEntitySafe, Entity>             ghToUnityEntityMap;
		private NativeHashMap<GhGameEntityHandle, GhGameEntitySafe> handleToSafeMap;

		private RegisterDeserializerSystem registerDeserializer;

		public NativeHashMap<CharBuffer256, ComponentTypeDetails>   typeDetailMapFromName;
		public NativeHashMap<GhComponentType, ComponentTypeDetails> typeDetailMapFromRow;

		private Dictionary<GhComponentType, (ICustomComponentDeserializer serializer, ICustomComponentArchetypeAttach attach)> componentTypeToDeserializer;

		protected override void OnCreate()
		{
			base.OnCreate();

			registerDeserializer = World.GetOrCreateSystem<RegisterDeserializerSystem>();
			ghToUnityEntityMap   = new NativeHashMap<GhGameEntitySafe, Entity>(64, Allocator.Persistent);
			handleToSafeMap      = new NativeHashMap<GhGameEntityHandle, GhGameEntitySafe>(64, Allocator.Persistent);

			typeDetailMapFromRow  = new NativeHashMap<GhComponentType, ComponentTypeDetails>(64, Allocator.Persistent);
			typeDetailMapFromName = new NativeHashMap<CharBuffer256, ComponentTypeDetails>(64, Allocator.Persistent);
			archetypeMap          = new Dictionary<uint, Archetype__>();

			defaultSpawnArchetype = EntityManager.CreateArchetype(typeof(ReplicatedGameEntity));

			componentTypeToDeserializer = new Dictionary<GhComponentType, (ICustomComponentDeserializer, ICustomComponentArchetypeAttach)>();
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

		public unsafe JobHandle OnNewMessage(ref DataBufferReader reader, bool isComponentStateWorth)
		{
			var       compressedSize     = reader.ReadValue<int>();
			var       uncompressedSize   = reader.ReadValue<int>();
			using var compressedMemory   = new NativeArray<byte>(compressedSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			using var uncompressedMemory = new NativeArray<byte>(uncompressedSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			reader.ReadDataSafe(compressedMemory);

			LZ4Codec.Decode((byte*) compressedMemory.GetUnsafePtr(), compressedSize,
				(byte*) uncompressedMemory.GetUnsafePtr(), uncompressedSize);

			var uncompressedReader = new DataBufferReader((byte*) uncompressedMemory.GetUnsafePtr(), uncompressedMemory.Length);
			return __onNewMessage(ref uncompressedReader, isComponentStateWorth);
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
			public GhGameEntitySafe GhGameEntity;
			public Entity           UnityEntity;
			public uint             NewArchetype;
		}

		[BurstCompile]
		public unsafe struct ReadEntitiesJob : IJob
		{
			public NativeArray<GhGameEntityHandle>                     entities;
			public NativeArray<uint>                                   entitiesArchetype;
			public NativeArray<uint>                                   entitiesVersion;
			public NativeHashMap<GhGameEntitySafe, Entity>             ghToUnityEntityMap;
			public NativeHashMap<GhGameEntityHandle, GhGameEntitySafe> handleToSafeMap;
			public EntityManager                                       EntityManager;
			public EntityArchetype                                     defaultSpawnArchetype;

			public NativeList<ArchetypeUpdate> archetypeUpdates;

			public void Execute()
			{
				for (uint ent = 1; ent < entitiesArchetype.Length; ent++)
				{
					handleToSafeMap.TryGetValue(new GhGameEntityHandle {Id = ent}, out var safe);
					if (entitiesArchetype[(int) ent] == 0 && ghToUnityEntityMap.TryGetValue(safe, out var unityEntity))
					{
						archetypeUpdates.Add(new ArchetypeUpdate
						{
							GhGameEntity = safe,
							UnityEntity  = unityEntity,
							NewArchetype = 0
						});

						ghToUnityEntityMap.Remove(safe);
						handleToSafeMap.Remove(new GhGameEntityHandle {Id = ent});
					}
				}

				for (var ent = 0; ent < entities.Length; ent++)
				{
					var handle = entities[ent];
					var safe   = new GhGameEntitySafe {Id = handle.Id, Version = entitiesVersion[(int) handle.Id]};

					var archetype       = entitiesArchetype[(int) handle.Id];
					var archetypeUpdate = false;
					if (!ghToUnityEntityMap.TryGetValue(safe, out var unityEntity))
					{
						ghToUnityEntityMap[safe] = unityEntity = EntityManager.CreateEntity(defaultSpawnArchetype);
						handleToSafeMap[handle]  = safe;
						EntityManager.SetComponentData(unityEntity, new ReplicatedGameEntity
						{
							Source      = safe,
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
							GhGameEntity = safe,
							UnityEntity  = unityEntity,
							NewArchetype = archetype
						});
					}
				}
			}
		}

		private unsafe JobHandle __onNewMessage(ref DataBufferReader reader, bool isComponentStateWorth)
		{
			/* flow map:
			 * 1. Component Type
			 * 2. Archetype
			 * 3. Entity
			 * 4. Component
			 */

			// ---- 1. Component Type
			//
			using var componentTypes = new NativeArray<GhComponentType>(reader.ReadValue<int>(), Allocator.TempJob);
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
			using var archetypes = new NativeArray<uint>(reader.ReadValue<int>(), Allocator.TempJob);
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
				return default;
			}

			// ---- 3. Entity
			//
			Profiler.BeginSample("ENtity");
			using var entities     = new NativeArray<GhGameEntityHandle>(reader.ReadValue<int>(), Allocator.TempJob);
			using var safeEntities = new NativeArray<GhGameEntitySafe>(entities.Length, Allocator.TempJob);
			if (entities.Length > 0)
			{
				// 3.1
				reader.ReadDataSafe(entities);

				// 3.2 archetypes
				using var entitiesArchetype = new NativeArray<uint>(reader.ReadValue<int>(), Allocator.TempJob);
				reader.ReadDataSafe(entitiesArchetype);
				// 3.3 versions
				// (a column will always have the same length as other columns...)
				using var entitiesVersion = new NativeArray<uint>(entitiesArchetype.Length, Allocator.TempJob);
				reader.ReadDataSafe(entitiesVersion);

				var safeEntitiesPtr = (GhGameEntitySafe*) safeEntities.GetUnsafePtr();
				for (var i = 0; i < entities.Length; i++)
				{
					var ent = entities[i];
					safeEntitiesPtr[i] = new GhGameEntitySafe {Id = ent.Id, Version = entitiesVersion[(int) ent.Id]};
				}

				using var archetypeUpdates = new NativeList<ArchetypeUpdate>(Allocator.TempJob);

				EntityManager.CompleteAllJobs();
				new ReadEntitiesJob
				{
					entities              = entities,
					entitiesArchetype     = entitiesArchetype,
					entitiesVersion       = entitiesVersion,
					ghToUnityEntityMap    = ghToUnityEntityMap,
					handleToSafeMap       = handleToSafeMap,
					EntityManager         = EntityManager,
					defaultSpawnArchetype = defaultSpawnArchetype,
					archetypeUpdates      = archetypeUpdates
				}.Run();

				if (archetypeUpdates.Length > 0)
					isComponentStateWorth = true;

				foreach (var (attach, _) in registerDeserializer.deserializerMap.Values)
				{
					attach.TryIncreaseCapacity(entitiesVersion.Length);
				}

				foreach (var update in archetypeUpdates)
				{
					var previousArchetype = 0u;
					if (EntityManager.TryGetComponentData(update.UnityEntity, out ReplicatedGameEntity previousReplicatedData))
						previousArchetype = previousReplicatedData.ArchetypeId;

					if (previousArchetype > 0)
						foreach (var attach in archetypeMap[previousArchetype].Attaches)
						{
							attach.OnEntityRemoved(EntityManager, update.GhGameEntity, update.UnityEntity);
						}

					if (update.NewArchetype > 0)
					{
						EntityManager.SetComponentData(update.UnityEntity, new ReplicatedGameEntity
						{
							Source      = update.GhGameEntity,
							ArchetypeId = update.NewArchetype
						});

						foreach (var attach in archetypeMap[update.NewArchetype].Attaches)
						{
							attach.OnEntityAdded(EntityManager, update.GhGameEntity, update.UnityEntity);
						}
					}
					else
						EntityManager.DestroyEntity(update.UnityEntity);

#if UNITY_EDITOR
					EntityManager.SetName(update.UnityEntity, $"GameHost Entity #{update.GhGameEntity.Id})");
#endif
				}
			}
			else
			{
				ghToUnityEntityMap.Clear();
				return default;
			}

			Profiler.EndSample();

			// If either there were no archetypes update or that the caller of this method decided to not include component update, skip it.
			if (!isComponentStateWorth)
				return default;

			// ---- 4. Component
			//
			// 4.1 Transforming gh entities to unity entities
			Profiler.BeginSample("Component");
			var outputEntities = new NativeArray<Entity>(entities.Length, Allocator.TempJob);
			Profiler.BeginSample("4.15 Convert Gh to Unity");
			for (var i = 0; i != entities.Length; i++)
				outputEntities[i] = ghToUnityEntityMap[safeEntities[i]];
			Profiler.EndSample();

			// 4.2 Deserialize Components
			var deps = new NativeList<JobHandle>(Allocator.Temp);
			for (var i = 0; i < componentTypes.Length; i++)
			{
				var skip                = reader.ReadValue<int>();
				var componentTypeBuffer = new DataBufferReader(reader, reader.CurrReadIndex, reader.Length);
				deps.Add(OnReadComponent(ref componentTypeBuffer, i, safeEntities, outputEntities, componentTypes));

				reader.CurrReadIndex += skip - sizeof(int);
			}

			Profiler.BeginSample("ScheduleBatched");
			JobHandle.ScheduleBatchedJobs();
			Profiler.EndSample();
			Profiler.EndSample();

			outputEntities.Dispose();
			return JobHandle.CombineDependencies(deps);
		}

		private HashSet<GhComponentType> warningSet = new HashSet<GhComponentType>();

		private JobHandle OnReadComponent(ref DataBufferReader reader, int index, NativeArray<GhGameEntitySafe> entities, NativeArray<Entity> output, NativeArray<GhComponentType> componentTypes)
		{
#if PROFILE_DEBUG
			Profiler.BeginSample("Get Deserializer");
#endif
			ICustomComponentArchetypeAttach attach       = null;
			ICustomComponentDeserializer    deserializer = null;
			if (!componentTypeToDeserializer.TryGetValue(componentTypes[index], out var outTuple))
			{
				var componentDetails = typeDetailMapFromRow[componentTypes[index]];
				var tuple            = registerDeserializer.Get(componentDetails.Size, componentDetails.Name);
				deserializer = tuple.deserializer;
				attach       = tuple.attach;

				componentTypeToDeserializer[componentTypes[index]] = (deserializer, attach);
			}
			else
			{
				deserializer = outTuple.serializer;
				attach       = outTuple.attach;
			}
#if PROFILE_DEBUG
			Profiler.EndSample();
#endif
			if (deserializer == null)
			{
				if (!warningSet.Contains(componentTypes[index]))
				{
					var componentDetails = typeDetailMapFromRow[componentTypes[index]];
					Debug.LogWarning($"Serializer not found for {componentDetails.Name} (size={componentDetails.Size}, row={componentDetails.Row})");
					warningSet.Add(componentTypes[index]);
				}

				return default;
			}

#if PROFILE_DEBUG
			Profiler.BeginSample(deserializer.GetType().Name);
#endif
			deserializer.BeginDeserialize(this);
			var jobHandle = deserializer.Deserialize(EntityManager, attach, entities, output, reader);
#if PROFILE_DEBUG
			Profiler.EndSample();
#endif

			return jobHandle;
		}

		public struct Archetype__
		{
			public NativeArray<uint>                     ComponentTypes;
			public List<ICustomComponentArchetypeAttach> Attaches;
		}
	}
}