using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace GameHost.ShareSimuWorldFeature
{
	public interface IValueDeserializer<TComponent>
		where TComponent : struct, IComponentData
	{
		public int Size { get; }

		void Deserialize(EntityManager em, NativeHashMap<GhGameEntity, Entity> ghEntityToUEntity, ref TComponent component, ref DataBufferReader reader);
	}

	public class CustomSingleDeserializer<TComponent, TValueDeserializer> : ICustomComponentDeserializer
		where TComponent : struct, IComponentData
		where TValueDeserializer : IValueDeserializer<TComponent>, new()
	{
		private ComponentDataFromEntity<TComponent> componentDataFromEntity;
		private NativeHashMap<GhGameEntity, Entity> ghToUnityEntityMap;

		private TValueDeserializer deserializer;

		public CustomSingleDeserializer()
		{
			deserializer = new TValueDeserializer();
		}

		public int Size => deserializer.Size;

		public void BeginDeserialize(SystemBase system)
		{
			componentDataFromEntity = system.GetComponentDataFromEntity<TComponent>();
			ghToUnityEntityMap      = system.World.GetExistingSystem<ReceiveSimulationWorldSystem>().ghToUnityEntityMap;
		}

		public void Deserialize(EntityManager entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, ref DataBufferReader reader)
		{
			// it's TagComponentBoard if size is 0, so nothing to read.
			if (Size == 0)
				return;

			var links = new NativeArray<GhComponentMetadata>(reader.ReadValue<int>(), Allocator.Temp);
			reader.ReadDataSafe(links);

			var componentCount = reader.ReadValue<int>();
			for (var ent = 0; ent < gameEntities.Length; ent++)
			{
				var entity = gameEntities[ent];
				if (links[(int) entity.Id].Null)
					continue;

				var current = componentDataFromEntity[output[ent]];
				deserializer.Deserialize(entityManager, ghToUnityEntityMap, ref current, ref reader);
				componentDataFromEntity[output[ent]] = current;
			}
		}
	}
}