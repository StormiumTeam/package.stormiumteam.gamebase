using System.Collections.Generic;
using GameHost.Native;
using package.stormiumteam.shared.ecs;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace GameHost.ShareSimuWorldFeature
{
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

		public void Deserialize(EntityManager entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, ref DataBufferReader reader)
		{
			// it's TagComponentBoard if size is 0, so nothing to read.
			if (Size == 0)
				return;

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