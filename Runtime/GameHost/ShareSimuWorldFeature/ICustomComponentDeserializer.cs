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
		void Deserialize(EntityManager entityManager, NativeArray<GhGameEntity> gameEntities, NativeArray<Entity> output, ref DataBufferReader reader);
	}

	public interface ICustomValueDeserializer<T>
	{
		public int Size { get; }
		
		void BeginDeserialize(SystemBase system);
		void Deserialize(EntityManager entityManager, GhGameEntity ghGameEntity, Entity entity, ref DataBufferReader reader);
	}
}