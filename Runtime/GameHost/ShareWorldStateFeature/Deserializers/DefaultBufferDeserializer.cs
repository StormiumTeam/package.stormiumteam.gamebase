using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace GameHost.ShareSimuWorldFeature
{
	public class DefaultBufferDeserializer<TComponent> : ICustomComponentDeserializer
		where TComponent : struct, IBufferElementData
	{
		private BufferFromEntity<TComponent> componentDataFromEntity;

		public DefaultBufferDeserializer()
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
			componentDataFromEntity = system.GetBufferFromEntity<TComponent>();
		}

		public void Deserialize(EntityManager entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, ref DataBufferReader reader)
		{
			var links = new NativeArray<GhComponentMetadata>(reader.ReadValue<int>(), Allocator.Temp);
			reader.ReadDataSafe(links);

			for (var ent = 0; ent < gameEntities.Length; ent++)
			{
				var entity = gameEntities[ent];
				if (links[(int) entity.Id].Null)
					continue;

				var buffer = componentDataFromEntity[output[ent]];
				buffer.Clear();

				var array = new NativeArray<TComponent>(reader.ReadValue<int>(), Allocator.Temp);
				reader.ReadDataSafe(array);

				buffer.CopyFrom(array);
			}
		}
	}
}