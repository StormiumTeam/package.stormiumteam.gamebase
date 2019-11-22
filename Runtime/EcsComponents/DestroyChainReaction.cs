using Revolution;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StormiumTeam.GameBase.Data
{
	public struct DestroyChainReaction : IComponentData
	{
		public Entity Target;

		public DestroyChainReaction(Entity target)
		{
			Target = target;
		}
	}

	public abstract class DestroyChainReactionSystemBase : ComponentSystem
	{
		private NativeList<Entity> m_ToDestroy;

		private void ForEach(Entity e, ref DestroyChainReaction destroyChainReaction)
		{
			if (destroyChainReaction.Target == default || !EntityManager.Exists(destroyChainReaction.Target))
				m_ToDestroy.Add(e);
		}

		private EntityQueryBuilder.F_ED<DestroyChainReaction> m_ForEachDelegate;
		private EntityQuery                                   m_Query;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ToDestroy       = new NativeList<Entity>(Allocator.Persistent);
			m_ForEachDelegate = ForEach;

			m_Query = GetEntityQuery(typeof(DestroyChainReaction), ComponentType.Exclude<ReplicatedEntity>());
		}

		protected override unsafe void OnUpdate()
		{
			m_ToDestroy.Clear();

			var entityType          = GetArchetypeChunkEntityType();
			var chunkComponentType0 = GetArchetypeChunkComponentType<DestroyChainReaction>(true);
			using (var chunks = m_Query.CreateArchetypeChunkArray(Allocator.TempJob))
			{
				foreach (var chunk in chunks)
				{
					var destroyChainReactionArray = chunk.GetNativeArray(chunkComponentType0).GetUnsafeReadOnlyPtr();
					var entityArray               = (Entity*) chunk.GetNativeArray(entityType).GetUnsafeReadOnlyPtr();

					for (int i = 0, count = chunk.Count; i < count; ++i)
					{
						var destroyChainReaction = UnsafeUtility.ReadArrayElement<DestroyChainReaction>(destroyChainReactionArray, i);
						if (destroyChainReaction.Target == default || !EntityManager.Exists(destroyChainReaction.Target))
							m_ToDestroy.Add(entityArray[i]);
					}
				}
			}

			if (m_ToDestroy.Length > 0)
			{
				EntityManager.DestroyEntity(m_ToDestroy);
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			m_ToDestroy.Dispose();
		}
	}

	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public class DestroyChainReactionSystemMainWorld : DestroyChainReactionSystemBase
	{
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.DeleteEntities))]
	public class DestroyChainReactionSystemClientServerWorld : DestroyChainReactionSystemBase
	{
	}
}