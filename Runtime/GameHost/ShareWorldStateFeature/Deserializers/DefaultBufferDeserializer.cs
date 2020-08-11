using RevolutionSnapshot.Core.Buffers;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

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

		[BurstCompile]
		private struct RunJob : IJob
		{
			public BufferFromEntity<TComponent> BufferFromEntity;
			public NativeArray<Entity>                Output;
			public NativeArray<GhGameEntity>                GameEntities;
			public DataBufferReader             Reader;

			public void Execute()
			{
				var links = new NativeArray<GhComponentMetadata>(Reader.ReadValue<int>(), Allocator.Temp);
				Reader.ReadDataSafe(links);

				var count = Reader.ReadValue<int>();
				for (var ent = 0; ent < GameEntities.Length; ent++)
				{
					var entity = GameEntities[ent];
					if (links[(int) entity.Id].Null)
						continue;

					var buffer = BufferFromEntity[Output[ent]];
					buffer.Clear();

					var rawData = new NativeArray<TComponent>(Reader.ReadValue<int>() / UnsafeUtility.SizeOf<TComponent>(), Allocator.Temp);
					Reader.ReadDataSafe(rawData);

					buffer.CopyFrom(rawData);
				}
			}
		}

		public JobHandle Deserialize(EntityManager entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, DataBufferReader reader)
		{
			return new RunJob
			{
				BufferFromEntity = componentDataFromEntity,
				Output           = output,
				GameEntities     = gameEntities,
				Reader           = reader
			}.Schedule();
		}
	}
}