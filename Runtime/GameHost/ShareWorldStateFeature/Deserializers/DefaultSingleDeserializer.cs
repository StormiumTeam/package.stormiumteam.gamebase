using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GameHost.Native;
using package.stormiumteam.shared;
using RevolutionSnapshot.Core.Buffers;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace GameHost.ShareSimuWorldFeature
{
	internal unsafe delegate void delegateSingleDeserialize(int size, void* parameters, ref DataBufferReader reader);
	
	[BurstCompile]
	public class DefaultSingleDeserializer<TComponent> : ICustomComponentDeserializer
		where TComponent : struct, IComponentData
	{
		private ComponentDataFromEntity<TComponent> componentDataFromEntity;

		public DefaultSingleDeserializer()
		{
			if (TypeManager.IsZeroSized(TypeManager.GetTypeIndex<TComponent>()))
			{
				Size = 0;
				return;
			}

			Size = UnsafeUtility.SizeOf<TComponent>();
		}

		public int Size { get; }
		
		public void BeginDeserialize(SystemBase system)
		{
			componentDataFromEntity = system.GetComponentDataFromEntity<TComponent>();
		}
		
		[BurstCompile]
		private struct RunJob : IJob
		{
			public ComponentDataFromEntity<TComponent> ComponentDataFromEntity;
			public NativeArray<GhGameEntity>           GameEntities;
			public NativeArray<Entity>                 Output;
			public DataBufferReader                    Reader;
			
			public void Execute()
			{
				var links = new NativeArray<GhComponentMetadata>(Reader.ReadValue<int>(), Allocator.Temp);
				Reader.ReadDataSafe(links);

				var components = new NativeArray<TComponent>(Reader.ReadValue<int>(), Allocator.Temp);
				var comp       = 0;
				Reader.ReadDataSafe(components);
				for (var ent = 0; ent < GameEntities.Length; ent++)
				{
					if (links[(int) GameEntities[ent].Id].Assigned == 0)
						continue;

					ComponentDataFromEntity[Output[ent]] = components[comp++];
				}
			}
		}

		public JobHandle Deserialize(EntityManager entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, DataBufferReader reader)
		{
			// Tag Component Board
			if (Size == 0)
				return default;

			var param = new RunJob
			{
				ComponentDataFromEntity = componentDataFromEntity,
				GameEntities            = gameEntities,
				Output                  = output,
				Reader                  = reader
			};
			return param.Schedule();
		}
	}

	public class DefaultArchetypeAttach<TComponent> : ICustomComponentArchetypeAttach
		where TComponent : struct
	{
		public readonly CharBuffer256 GameHostType;
		public readonly ComponentType UnityType;

		public DefaultArchetypeAttach(string ghType)
		{
			GameHostType = CharBufferUtility.Create<CharBuffer256>(ghType);
			UnityType    = typeof(TComponent);
		}

		public string[] RegisterTypes()
		{
			return new[] {GameHostType.Span.ToString()};
		}

		public bool CanAttachToArchetype(NativeArray<GhComponentType> componentTypes, NativeHashMap<CharBuffer256, ComponentTypeDetails> detailMap)
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
			if (!entityManager.HasComponent(output, UnityType))
				entityManager.AddComponent(output, UnityType);
		}

		public void OnEntityRemoved(EntityManager entityManager, GhGameEntity ghEntity, Entity output)
		{
			if (entityManager.HasComponent(output, UnityType))
				entityManager.RemoveComponent(output, UnityType);
		}
	}
}