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
			public NativeArray<GhGameEntitySafe>                GameEntities;
			public NativeArray<bool>                Valid;
			public DataBufferReader             Reader;

			public void Execute()
			{
				var count = Reader.ReadValue<int>();
				for (var ent = 0; ent < GameEntities.Length; ent++)
				{
					var entity = GameEntities[ent];
					if (!Valid[(int) entity.Id])
						continue;

					var buffer = BufferFromEntity[Output[ent]];
					buffer.Clear();

					var rawData = new NativeArray<TComponent>(Reader.ReadValue<int>() / UnsafeUtility.SizeOf<TComponent>(), Allocator.Temp);
					Reader.ReadDataSafe(rawData);

					buffer.CopyFrom(rawData);
				}
			}
		}

		public JobHandle Deserialize(EntityManager                 entityManager, ICustomComponentArchetypeAttach attach,
		                             NativeArray<GhGameEntitySafe> gameEntities,  NativeArray<Entity>             output,
		                             DataBufferReader              reader)
		{
			return new RunJob
			{
				BufferFromEntity = componentDataFromEntity,
				Output           = output,
				Valid            = attach.GetValidHandles(),
				GameEntities     = gameEntities,
				Reader           = reader
			}.Schedule();
		}
	}
}