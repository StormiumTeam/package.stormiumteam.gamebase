using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GameHost.Native;
using package.stormiumteam.shared;
using RevolutionSnapshot.Core.Buffers;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

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

		private FunctionPointer<delegateSingleDeserialize> fp;
		private delegateSingleDeserialize                  deserialize;

		public unsafe void BeginDeserialize(SystemBase system)
		{
			componentDataFromEntity = system.GetComponentDataFromEntity<TComponent>();
			if (!fp.IsCreated)
			{
				fp          = BurstCompiler.CompileFunctionPointer((delegateSingleDeserialize) DeserializeMethod);
				deserialize = fp.Invoke;
			}
		}

		private struct Parameters
		{
			public ComponentDataFromEntity<TComponent> ComponentDataFromEntity;
			public NativeArray<GhGameEntity>           GameEntities;
			public NativeArray<Entity>                 Output;
		}

		[BurstCompile]
		private static unsafe void DeserializeMethod(int size, void* parameters, ref DataBufferReader reader)
		{
			// it's TagComponentBoard if size is 0, so nothing to read.
			if (size == 0)
				return;

			var param = Unsafe.Read<Parameters>(parameters);
			var links = new NativeArray<GhComponentMetadata>(reader.ReadValue<int>(), Allocator.Temp);
			reader.ReadDataSafe(links);

			var components = new NativeArray<TComponent>(reader.ReadValue<int>(), Allocator.Temp);
			var comp       = 0;
			reader.ReadDataSafe(components);
			for (var ent = 0; ent < param.GameEntities.Length; ent++)
			{
				if (links[(int) param.GameEntities[ent].Id].Assigned == 0)
					continue;

				param.ComponentDataFromEntity[param.Output[ent]] = components[comp++];
			}
		}

		public unsafe void Deserialize(EntityManager entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, ref DataBufferReader reader)
		{
			var param = new Parameters
			{
				ComponentDataFromEntity = componentDataFromEntity,
				GameEntities            = gameEntities,
				Output                  = output
			};

			var alloc = UnsafeAllocation.From(ref param);
			deserialize(Size, alloc.Data, ref reader);
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

		public bool CanAttachToArchetype(NativeArray<GhComponentType> componentTypes, Dictionary<CharBuffer256, ComponentTypeDetails> detailMap)
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