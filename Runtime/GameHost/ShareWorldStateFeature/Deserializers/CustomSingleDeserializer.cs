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

		void Deserialize(EntityManager em, NativeHashMap<GhGameEntitySafe, Entity> ghEntityToUEntity, ref TComponent component, ref DataBufferReader reader);
	}

	[BurstCompile]
	public class CustomSingleDeserializer<TComponent, TValueDeserializer> : ICustomComponentDeserializer
		where TComponent : struct, IComponentData
		where TValueDeserializer : struct, IValueDeserializer<TComponent>
	{
		private ComponentDataFromEntity<TComponent> componentDataFromEntity;
		private NativeHashMap<GhGameEntitySafe, Entity> ghToUnityEntityMap;

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
			public ComponentDataFromEntity<TComponent>     ComponentDataFromEntity;
			public NativeArray<GhGameEntitySafe>           GameEntities;
			public NativeArray<Entity>                     Output;
			public NativeArray<bool>                       Valid;
			public EntityManager                           EntityManager;
			public NativeHashMap<GhGameEntitySafe, Entity> GhToUnityEntityMap;

			public DataBufferReader Reader;

			public unsafe void Execute()
			{
				var componentcount = Reader.ReadValue<int>();
				if (componentcount == 0)
					return;
				
				var deserializer   = default(TValueDeserializer);
				for (var ent = 0; ent < GameEntities.Length; ent++)
				{
					var entity = GameEntities[ent];
					if (!Valid[(int) entity.Id])
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

		public unsafe JobHandle Deserialize(EntityManager                 entityManager, ICustomComponentArchetypeAttach attach,
		                                    NativeArray<GhGameEntitySafe> gameEntities,  NativeArray<Entity>             output,
		                                    DataBufferReader              reader)
		{
			if (Size == 0)
				return default;

			var param = new RunJob
			{
				ComponentDataFromEntity = componentDataFromEntity,
				GameEntities            = gameEntities,
				Valid                   = attach.GetValidHandles(),
				Output                  = output,
				EntityManager           = entityManager,
				GhToUnityEntityMap      = ghToUnityEntityMap,

				Reader = reader
			};
			return param.Schedule();
		}
	}
}