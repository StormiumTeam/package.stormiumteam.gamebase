using System.Collections.Generic;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.GameHost.Simulation;
using Unity.Collections;
using Unity.Entities;

namespace DefaultNamespace
{
	public interface ICustomComponentDeserializer
	{
		public int Size { get; }

		void BeginDeserialize(SystemBase system);
		void Deserialize(EntityManager   entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, ref DataBufferReader reader);
	}

	public interface ICustomComponentArchetypeAttach
	{
		string[] RegisterTypes();

		bool CanAttachToArchetype(NativeArray<GhComponentType> componentTypes, Dictionary<string, ComponentTypeDetails> detailMap);

		void OnEntityAdded(EntityManager   entityManager, GhGameEntity ghEntity, Entity output);
		void OnEntityRemoved(EntityManager entityManager, GhGameEntity ghEntity, Entity output);
	}
}