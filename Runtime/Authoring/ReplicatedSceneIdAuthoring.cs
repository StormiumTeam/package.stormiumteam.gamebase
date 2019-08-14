using System.Collections.Generic;
using DefaultNamespace;
using package.stormiumteam.shared.ecs;
using Scripts.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace Runtime.Authoring
{
	public class ReplicatedSceneIdAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public byte[] NetId;
		public ReplicatedSceneIdHolder Parent;

		public unsafe void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			if ((Parent?.NetId ?? NetId) == null)
			{
				Debug.LogError("[IMPORTANT] Null 'NetID' on " + gameObject.name);
				return;
			}

			fixed (byte* ptr = Parent?.NetId ?? NetId)
			{
				var v = new ReplSceneId();
				UnsafeUtility.MemCpy(&v, ptr, sizeof(ReplSceneId));

				dstManager.AddComponentData(entity, v);
				dstManager.AddComponent(entity, typeof(GhostComponent));
			}
		}

#if UNITY_EDITOR
		public static Dictionary<byte[], GameObject> netGuidMap = new Dictionary<byte[], GameObject>();
		
		private void OnValidate()
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