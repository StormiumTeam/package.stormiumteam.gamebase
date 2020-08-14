using Unity.Collections;
using Unity.Entities;

namespace StormiumTeam.GameBase.Data
{
	// TODO: This component should exist in GameHost side and be synchroznied
	public struct ExecutingServerMap : IComponentData
	{
		public FixedString512 Key;
	}

	// TODO: This system should be moved into GameHost once the netcode will be made
	/*public class ServerSynchronizeMap : ComponentSystem
	{
		private EntityQuery m_ExecutingMapQuery;
		private Entity      m_ServerMapEntity;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ExecutingMapQuery = GetEntityQuery(typeof(ExecutingMapData));
			m_ServerMapEntity   = EntityManager.CreateEntity(typeof(ExecutingServerMap), typeof(GhostEntity));
		}

		protected override void OnUpdate()
		{
			if (m_ExecutingMapQuery.IsEmptyIgnoreFilter)
				return;

			var entity  = m_ExecutingMapQuery.GetSingletonEntity();
			var currMap = EntityManager.GetComponentData<ExecutingMapData>(entity);

			EntityManager.SetComponentData(m_ServerMapEntity, new ExecutingServerMap {Key = currMap.Key});
		}
	}*/
}