using GameHost.Native;
using RevolutionSnapshot.Core.Buffers;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

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
			public NativeArray<GhGameEntitySafe>       GameEntities;
			public NativeArray<bool>                   Valid;
			public NativeArray<Entity>                 Output;
			public DataBufferReader                    Reader;

			public void Execute()
			{
				var components = new NativeArray<TComponent>(Reader.ReadValue<int>(), Allocator.Temp);
				var comp       = 0;
				Reader.ReadDataSafe(components);
				for (var ent = 0; ent < GameEntities.Length; ent++)
				{
					if (!Valid[(int) GameEntities[ent].Id])
						continue;

					ComponentDataFromEntity[Output[ent]] = components[comp++];
				}
			}
		}

		public JobHandle Deserialize(EntityManager                 entityManager, ICustomComponentArchetypeAttach attach,
		                             NativeArray<GhGameEntitySafe> gameEntities,  NativeArray<Entity>             output,
		                             DataBufferReader              reader)
		{
			// Tag Component Board
			if (Size == 0)
				return default;

			var param = new RunJob
			{
				ComponentDataFromEntity = componentDataFromEntity,
				GameEntities            = gameEntities,
				Valid                   = attach.GetValidHandles(),
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

		private NativeArray<bool> validHandles;

		public DefaultArchetypeAttach(string ghType)
		{
			GameHostType = CharBufferUtility.Create<CharBuffer256>(ghType);
			UnityType    = typeof(TComponent);

			validHandles = new NativeArray<bool>(1, Allocator.Persistent);
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

		public void TryIncreaseCapacity(int size)
		{
			if (validHandles.Length < size)
			{
				var previousHandles = validHandles;
				validHandles = new NativeArray<bool>(size, Allocator.Persistent);
				previousHandles.CopyTo(validHandles.GetSubArray(0, previousHandles.Length));

				previousHandles.Dispose();
			} 
		}

		public void OnEntityAdded(EntityManager entityManager, GhGameEntitySafe ghEntity, Entity output)
		{
			validHandles[(int) ghEntity.Id] = true;
			
			if (!entityManager.HasComponent(output, UnityType))
				entityManager.AddComponent(output, UnityType);
		}

		public void OnEntityRemoved(EntityManager entityManager, GhGameEntitySafe ghEntity, Entity output)
		{
			validHandles[(int) ghEntity.Id] = false;
			
			if (entityManager.HasComponent(output, UnityType))
				entityManager.RemoveComponent(output, UnityType);
		}

		public NativeArray<bool> GetValidHandles()
		{
			return validHandles;
		}

		~DefaultArchetypeAttach()
		{
			if (validHandles.IsCreated)
				validHandles.Dispose();
		}
	}
}