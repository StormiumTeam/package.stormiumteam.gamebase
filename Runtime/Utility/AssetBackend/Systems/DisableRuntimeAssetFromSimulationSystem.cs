using System.Collections.Generic;
using StormiumTeam.GameBase.Utility.AssetBackend.Components;
using Unity.Collections;
using Unity.Entities;

namespace StormiumTeam.GameBase.Utility.AssetBackend.Systems
{
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class DisableRuntimeAssetFromSimulationSystem : SystemBase
	{
		private List<(bool disableGo, bool returnPresentation, RuntimeAssetBackendBase be)> m_List; // how should I even name it lol
		private EntityQuery                                                                 m_Query;

		protected override void OnCreate()
		{
			m_Query = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(RuntimeAssetDisable), typeof(RuntimeAssetBackendBase.RuntimeAssetDetection)}
				//Any = new ComponentType[]{typeof(ModelParent)}
			});

			m_List = new List<(bool disableGo, bool returnPresentation, RuntimeAssetBackendBase)>();
		}

		protected override void OnUpdate()
		{
			m_List.Clear();
			using (var chunks = m_Query.CreateArchetypeChunkArray(Allocator.TempJob))
			{
				foreach (var chunk in chunks)
				{
					var disableDataArray = chunk.GetNativeArray(GetComponentTypeHandle<RuntimeAssetDisable>(true));
					var detectionArray   = chunk.GetManagedComponentAccessor(EntityManager.GetComponentTypeHandle<RuntimeAssetBackendBase.RuntimeAssetDetection>(false), EntityManager);
					for (int ent = 0, count = chunk.Count; ent != count; ent++)
					{
						var disable     = disableDataArray[ent];
						var modelParent = default(Entity);

						if (!disable.IgnoreParent && chunk.Has(GetComponentTypeHandle<ModelParent>(true)))
						{
							var modelParentArray = chunk.GetNativeArray(GetComponentTypeHandle<ModelParent>(true));
							modelParent = modelParentArray[ent].Parent;
						}

						if (!disable.IgnoreParent && modelParent == default || EntityManager.Exists(modelParent)) continue;

						var backend = detectionArray[ent].GetComponent<RuntimeAssetBackendBase>();
						m_List.Add((disable.DisableGameObject, disable.ReturnPresentation, backend));
					}
				}
			}

			foreach (var (disableGo, returnPresentation, backend) in m_List) backend.Return(disableGo, returnPresentation);
		}
	}
}