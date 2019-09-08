using System.Collections.Generic;
using MonoComponents;
using Revolution.Utils;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace StormiumTeam.GameBase.Modules
{
	public class ModuleGetAssetFromGuid : BaseSystemModule
	{
		public delegate void OnLoad(object asset, bool isSceneObject);
		
		public override ModuleUpdateType UpdateType => ModuleUpdateType.MainThread;

		private List<(AsyncOperationHandle, OnLoad)> m_Handles;
		
		protected override void OnEnable()
		{
			m_Handles = new List<(AsyncOperationHandle, OnLoad)>(16);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
			foreach (var (handle, onLoad) in m_Handles)
			{
				if (!handle.IsDone)
					continue;

				onLoad(handle.Result, false);
			}
		}

		protected override void OnDisable()
		{
			m_Handles.Clear();
			m_Handles = null;
		}

		public void GetGameObject(AssetGuid guid, OnLoad onLoad)
		{
			GetGameObject(guid.value, onLoad);
		}
		
		public void GetGameObject(ReplicatedGuid guid, OnLoad onLoad)
		{
			GameObject sceneGameObject = null;
			if ((sceneGameObject = StaticSceneResourceHolder.Get(guid)) != null)
			{
				onLoad(sceneGameObject, true);
				return;
			}
			
			m_Handles.Add((Addressables.LoadAssetAsync<GameObject>(guid), onLoad));
		}
	}
}