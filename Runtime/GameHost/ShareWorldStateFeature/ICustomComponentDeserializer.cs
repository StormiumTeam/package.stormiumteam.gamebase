using System.Collections.Generic;
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

		void BeginDeserialize(SystemBase system);
		JobHandle Deserialize(EntityManager   entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, DataBufferReader reader);
	}

	public interface ICustomComponentArchetypeAttach
	{
		string[] RegisterTypes();

		bool CanAttachToArchetype(NativeArray<GhComponentType> componentTypes, NativeHashMap<CharBuffer256, ComponentTypeDetails> detailMap);

		void OnEntityAdded(EntityManager   entityManager, GhGameEntity ghEntity, Entity output);
		void OnEntityRemoved(EntityManager entityManager, GhGameEntity ghEntity, Entity output);
	}
}