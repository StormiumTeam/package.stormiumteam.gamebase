using RevolutionSnapshot.Core.Buffers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace GameHost.ShareSimuWorldFeature
{
	public interface IValueDeserializer<TComponent>
		where TComponent : struct, IComponentData
	{
		public int Size { get; }

		void Deserialize(EntityManager em, NativeHashMap<GhGameEntity, Entity> ghEntityToUEntity, ref TComponent component, ref DataBufferReader reader);
	}

	[BurstCompile]
	public class CustomSingleDeserializer<TComponent, TValueDeserializer> : ICustomComponentDeserializer
		where TComponent : struct, IComponentData
		where TValueDeserializer : struct, IValueDeserializer<TComponent>
	{
		private ComponentDataFromEntity<TComponent> componentDataFromEntity;
		private NativeHashMap<GhGameEntity, Entity> ghToUnityEntityMap;

		private TValueDeserializer deserializer;

		public CustomSingleDeserializer()
		{
			deserializer = new TValueDeserializer();
		}

		public int Size => deserializer.Size;
		
		public unsafe void BeginDeserialize(SystemBase system)
		{
			componentDataFromEntity = system.GetComponentDataFromEntity<TComponent>();
			ghToUnityEntityMap      = system.World.GetExistingSystem<ReceiveSimulationWorldSystem>().ghToUnityEntityMap;
		}

		[BurstCompile]
		private struct RunJob : IJob
		{
			public ComponentDataFromEntity<TComponent> ComponentDataFromEntity;
			public NativeArray<GhGameEntity>           GameEntities;
			public NativeArray<Entity>                 Output;
			public EntityManager                       EntityManager;
			public NativeHashMap<GhGameEntity, Entity> GhToUnityEntityMap;

			public DataBufferReader Reader;

			public unsafe void Execute()
			{
				var links  = new NativeArray<GhComponentMetadata>(Reader.ReadValue<int>(), Allocator.Temp);
				Reader.ReadDataSafe(links);

				var componentcount = Reader.ReadValue<int>();
				var deserializer   = default(TValueDeserializer);
				for (var ent = 0; ent < GameEntities.Length; ent++)
				{
					var entity = GameEntities[ent];
					if (links[(int) entity.Id].Null)
						continue;

					TComponent current = default;
					if (!ComponentDataFromEntity.HasComponent(Output[ent]))
					{
						deserializer.Deserialize(EntityManager, GhToUnityEntityMap, ref current, ref Reader);

						continue;
					}

					current = ComponentDataFromEntity[Output[ent]];
					deserializer.Deserialize(EntityManager, GhToUnityEntityMap, ref current, ref Reader);
					ComponentDataFromEntity[Output[ent]] = current;
				}
			}
		}

		public unsafe JobHandle Deserialize(EntityManager entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, DataBufferReader reader)
		{
			if (Size == 0)
				return default;
			
			var param = new RunJob
			{
				ComponentDataFromEntity = componentDataFromEntity,
				GameEntities            = gameEntities,
				Output                  = output,
				EntityManager           = entityManager,
				GhToUnityEntityMap      = ghToUnityEntityMap,
				
				Reader = reader
			};
			return param.Schedule();
		}
	}
}