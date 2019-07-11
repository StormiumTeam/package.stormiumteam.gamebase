using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Runtime.Systems
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class DisableRuntimeAssetFromSimulationSystem : ComponentSystem
	{
		private EntityQuery m_Query;

		protected override void OnCreate()
		{
			m_Query = GetEntityQuery(typeof(RuntimeAssetDisable), typeof(ModelParent), typeof(Transform));
		}

		protected override void OnUpdate()
		{
			using (var chunks = m_Query.CreateArchetypeChunkArray(Allocator.TempJob))
			{
				foreach (var chunk in chunks)
				{
					var disableDataArray = chunk.GetNativeArray(GetArchetypeChunkComponentType<RuntimeAssetDisable>(true));
					var modelParentArray = chunk.GetNativeArray(GetArchetypeChunkComponentType<ModelParent>(true));
					var transformArray = chunk.GetComponentObjects(GetArchetypeChunkComponentType<Transform>(), EntityManager);
					for (int ent = 0, count = chunk.Count; ent != count; ent++)
					{
						var modelParent = modelParentArray[ent];
						if (EntityManager.Exists(modelParent.Parent))
							continue;
						
						var disable = disableDataArray[ent];
						var backend = transformArray[ent].GetComponent<RuntimeAssetBackendBase>();
						
						backend.Return(disable.DisableGameObject, disable.ReturnPresentation);
						
						Debug.Log("ye");
					}
				}
			}
		}
	}
}