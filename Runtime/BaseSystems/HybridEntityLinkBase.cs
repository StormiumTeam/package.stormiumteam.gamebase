using StormiumTeam.GameBase.Modules;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.BaseSystems
{
	public abstract class HybridEntityLinkBase<TBackend> : AbsGameBaseSystem
		where TBackend : MonoBehaviour
	{
		private EntityQuery                   m_EntityQuery;
		private GetAllBackendModule<TBackend> m_GetAllBackendModule;

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_GetAllBackendModule);
			m_EntityQuery = GetQuery();
		}

		protected override void OnUpdate()
		{
			using (var entities = m_EntityQuery.ToEntityArray(Allocator.TempJob))
			{
				m_GetAllBackendModule.TargetEntities = entities;
				m_GetAllBackendModule.Update(default).Complete();

				if (m_GetAllBackendModule.BackendWithoutModel.Length > 0
				    || m_GetAllBackendModule.MissingTargets.Length > 0)
					OnResult(m_GetAllBackendModule.BackendWithoutModel, m_GetAllBackendModule.MissingTargets);
			}
		}

		public abstract EntityQuery GetQuery();
		public abstract void        OnResult(NativeArray<Entity> backendWithoutEntity, NativeArray<Entity> entityWithoutBackend);
	}
}