
using DefaultNamespace;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Runtime.Authoring
{
	public class ReplicatedSceneIdHolder : MonoBehaviour
	{
		public byte[] NetId;

		private unsafe void Awake()
		{
			if (NetId == null || NetId.Length == 0)
			{
				Debug.LogError("Null NetId!");
				return;
			}
			
			fixed (byte* ptr = NetId)
			{
				var v = new ReplicatedGuid();
				UnsafeUtility.MemCpy(&v, ptr, sizeof(ReplicatedGuid));
				
				Debug.Log($"HOLDER;\nB0: {v.b0}\nB1: {v.b1}\nB2: {v.b2}\nB3: {v.b3}");
			}
		}
		
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