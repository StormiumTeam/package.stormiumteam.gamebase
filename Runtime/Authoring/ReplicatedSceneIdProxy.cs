using DefaultNamespace;
using Unity.Entities;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace StormiumTeam.GameBase.Authoring
{
	public class ReplicatedSceneIdProxy : ComponentDataProxy<AssetGuid>
	{
		public byte[]                  NetId;
		public ReplicatedSceneIdHolder Parent;


#if UNITY_EDITOR
		private unsafe void OnValidate()
		{
			if (EditorApplication.isPlaying)
				return;

			PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(this);
			if (prefabType != PrefabAssetType.NotAPrefab && prefabType != PrefabAssetType.MissingAsset)
			{
				NetId = null;
			}
			else
				SetUniqueNetID();

			if (NetId != null)
			{
				fixed (byte* ptr = Parent?.NetId ?? NetId)
				{
					var v = new AssetGuid();
					UnsafeUtility.MemCpy(&v, ptr, sizeof(AssetGuid));

					Value = v;
				}
			}
		}

		private void SetUniqueNetID()
		{
			var netGuidMap = ReplicatedSceneIdAuthoring.netGuidMap;

			// Generate new if fresh object
			if (NetId == null || NetId.Length == 0)
			{
				var guid = System.Guid.NewGuid();
				NetId = guid.ToByteArray();
				EditorSceneManager.MarkSceneDirty(gameObject.scene);
			}

			// If we are the first add us
			if (!netGuidMap.ContainsKey(NetId))
			{
				netGuidMap[NetId] = gameObject;
				return;
			}


			// Our guid is known and in use by another object??
			var oldReg = netGuidMap[NetId];
			if (oldReg != null && oldReg.GetInstanceID() != gameObject.GetInstanceID())
			{
				// If actually *is* another ReplEnt that has our netID, *then* we give it up (usually happens because of copy / paste)
				NetId = System.Guid.NewGuid().ToByteArray();
				EditorSceneManager.MarkSceneDirty(gameObject.scene);
			}

			netGuidMap[NetId] = gameObject;
		}
#endif
	}
}