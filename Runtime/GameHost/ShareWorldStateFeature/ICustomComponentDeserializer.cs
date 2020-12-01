using GameHost.Native;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace GameHost.ShareSimuWorldFeature
{
	public interface ICustomComponentDeserializer
	{
		public int Size { get; }

		void      BeginDeserialize(SystemBase system);

		JobHandle Deserialize(EntityManager                 entityManager, ICustomComponentArchetypeAttach attach,
		                      NativeArray<GhGameEntitySafe> gameEntities,  NativeArray<Entity>             output,
		                      DataBufferReader              reader);
	}

	public interface ICustomComponentArchetypeAttach
	{
		string[] RegisterTypes();

		bool CanAttachToArchetype(NativeArray<GhComponentType> componentTypes, NativeHashMap<CharBuffer256, ComponentTypeDetails> detailMap);

		void TryIncreaseCapacity(int size);

		void              OnEntityAdded(EntityManager   entityManager, GhGameEntitySafe ghEntity, Entity output);
		void              OnEntityRemoved(EntityManager entityManager, GhGameEntitySafe ghEntity, Entity output);
		NativeArray<bool> GetValidHandles();
	}
}