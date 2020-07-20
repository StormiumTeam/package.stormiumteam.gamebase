using package.stormiumteam.shared.ecs;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.GameHost.Simulation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace DefaultNamespace
{
	public class DefaultSingleDeserializerWithCustomValue<TInner, TComponent> : ICustomComponentDeserializer
		where TInner : ICustomValueDeserializer<TComponent>, new()
		where TComponent : struct, IComponentData
	{
		public int Size =>
			inner.Size > 0
				? inner.Size
				: UnsafeUtility.SizeOf<TComponent>();

		public void BeginDeserialize(SystemBase system)
		{
		}

		public void Deserialize(EntityManager entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, ref DataBufferReader reader)
		{
			var length = gameEntities.Length;
			for (var ent = 0; ent != length; ent++)
			{
				inner.Deserialize(entityManager, gameEntities[ent], output[ent], ref reader);
			}
		}

		private TInner inner = new TInner();

		public class DefaultInner : ICustomValueDeserializer<TComponent>
		{
			public int Size => UnsafeUtility.SizeOf<TComponent>();

			public void BeginDeserialize(SystemBase system)
			{
			}

			public void Deserialize(EntityManager entityManager, GhGameEntity ghGameEntity, Entity entity, ref DataBufferReader reader)
			{
				entityManager.SetOrAddComponentData(entity, reader.ReadValue<TComponent>());
			}
		}
	}
}