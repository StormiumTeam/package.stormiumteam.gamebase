using package.StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	[UpdateAfter(typeof(DestroyChainReactionSystemClientServerWorld))]
	public class UpdateActionContainerSystem : GameBaseSystem
	{
		private void ForEachClear(DynamicBuffer<ActionContainer> buffer)
		{
			buffer.Clear();
		}

		private void ForEachUpdate(Entity entity, ref Owner owner)
		{
			if (!EntityManager.Exists(owner.Target))
			{
				Debug.LogError("Owner doesn't exist anymore.");
				return;
			}

			var newBuffer = EntityManager.HasComponent(owner.Target, typeof(ActionContainer))
				? PostUpdateCommands.SetBuffer<ActionContainer>(owner.Target)
				: PostUpdateCommands.AddBuffer<ActionContainer>(owner.Target);

			newBuffer.Add(new ActionContainer(entity));
		}

		private EntityQueryBuilder.F_B<ActionContainer> m_ForEachClear;
		private EntityQueryBuilder                      m_ActionContainerBuilder;

		private EntityQuery m_DataQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ForEachClear           = ForEachClear;
			m_ActionContainerBuilder = Entities.WithAll<ActionContainer>();

			m_DataQuery = GetEntityQuery(typeof(Owner), typeof(ActionDescription));
		}

		protected override void OnUpdate()
		{
			m_ActionContainerBuilder.ForEach(m_ForEachClear);

			using (var entities = m_DataQuery.ToEntityArray(Allocator.TempJob))
			using (var owners = m_DataQuery.ToComponentDataArray<Owner>(Allocator.TempJob))
			{
				for (var ent = 0; ent != entities.Length; ent++)
				{
					if (!EntityManager.HasComponent(owners[ent].Target, typeof(ActionContainer)))
						continue;
					
					var buffer = EntityManager.GetBuffer<ActionContainer>(owners[ent].Target);
					if (!buffer.IsCreated)
					{

						Debug.LogWarning(owners[ent].Target + " no action buffer");
						continue;
					}
					buffer.Add(new ActionContainer(entities[ent]));
				}
			}
		}
	}
}