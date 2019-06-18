using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

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
			if (destroyChainReaction.Target == default)
				return;
			
			if (!EntityManager.Exists(destroyChainReaction.Target))
				m_ToDestroy.Add(e);
		}

		private EntityQueryBuilder.F_ED<DestroyChainReaction> m_ForEachDelegate;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ToDestroy       = new NativeList<Entity>(Allocator.TempJob);
			m_ForEachDelegate = ForEach;
		}

		protected override void OnUpdate()
		{
			m_ToDestroy.Clear();
			Entities.ForEach(m_ForEachDelegate);
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

	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	public class DestroyChainReactionSystemClientServerWorld : DestroyChainReactionSystemBase
	{
	}
}