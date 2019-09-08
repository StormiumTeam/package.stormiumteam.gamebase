using System;
using System.Collections.Generic;
using Revolution.Utils;
using StormiumTeam.GameBase;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace MonoComponents
{
	[ExecuteInEditMode]
	public class StaticSceneResourceHolder : MonoBehaviour
	{
		public static Dictionary<ReplicatedGuid, AsyncAssetPool<GameObject>> GuidToGameObject = new Dictionary<ReplicatedGuid, AsyncAssetPool<GameObject>>();
		public static Dictionary<string, AsyncAssetPool<GameObject>>         IdToGameObject   = new Dictionary<string, AsyncAssetPool<GameObject>>();

		public static GameObject Get(ReplicatedGuid guid)
		{
			GuidToGameObject.TryGetValue(guid, out var go);
			return go?.LoadedAsset;
		}

		public static GameObject Get(string id)
		{
			IdToGameObject.TryGetValue(id, out var go);
			return go?.LoadedAsset;
		}

		public static AsyncAssetPool<GameObject> GetPool(ReplicatedGuid guid)
		{
			GuidToGameObject.TryGetValue(guid, out var go);
			return go;
		}

		public static AsyncAssetPool<GameObject> GetPool(string id)
		{
			IdToGameObject.TryGetValue(id, out var go);
			return go;
		}

		private unsafe void OnEnable()
		{
			AsyncAssetPool<GameObject> CreatePool(GameObject origin)
			{
				return new AsyncAssetPool<GameObject>(origin, null);
			}

			// first get guid from children that already have one...
			foreach (Transform child in transform)
			{
				var sceneAssetGuid = child.gameObject.GetComponent<SceneAssetAuthoring>();
				if (sceneAssetGuid != null)
				{
					AsyncAssetPool<GameObject> otherGo = null;
					if (sceneAssetGuid.GetValidAddressableKey() != null)
					{
						if (!string.IsNullOrEmpty(sceneAssetGuid.id) && !IdToGameObject.TryGetValue(sceneAssetGuid.id, out otherGo))
						{
							IdToGameObject[sceneAssetGuid.id] = new AsyncAssetPool<GameObject>((string) sceneAssetGuid.GetValidAddressableKey());
						}
						else if (otherGo != null)
						{
							Debug.LogWarning($"{otherGo.LoadedAsset.name} had the same id as {gameObject.name}");
						}

						continue;
					}

					var guid = sceneAssetGuid.assetGuid;
					if (guid.Equals(default))
						continue;

					GuidToGameObject[sceneAssetGuid.assetGuid] = CreatePool(child.gameObject);

					if (!string.IsNullOrEmpty(sceneAssetGuid.id) && !IdToGameObject.TryGetValue(sceneAssetGuid.id, out otherGo))
					{
						IdToGameObject[sceneAssetGuid.id] = CreatePool(child.gameObject);
					}
					else if (otherGo != null)
					{
						Debug.LogWarning($"{otherGo.LoadedAsset.name} had the same id as {gameObject.name}");
					}
				}
			}

			if (Application.isPlaying)
			{
				// Disable child gameObject (except us)
				foreach (Transform child in transform)
				{
					child.gameObject.SetActive(false);
				}

				return;
			}

			// Assign guid to children that don't have it yet...
			foreach (Transform child in transform)
			{
				var sceneAssetGuid = child.gameObject.GetComponent<SceneAssetAuthoring>();
				var generate       = false;
				if (sceneAssetGuid == null)
				{
					sceneAssetGuid = child.gameObject.AddComponent<SceneAssetAuthoring>();
					generate       = true;
				}
				else if (sceneAssetGuid.assetGuid.Equals(default))
					generate = true;

				if (!generate)
					continue;

				var generatedGuid = Guid.NewGuid();
				fixed (byte* ptr = generatedGuid.ToByteArray())
				{
					var v = new ReplicatedGuid();
					UnsafeUtility.MemCpy(&v, ptr, sizeof(ReplicatedGuid));

					sceneAssetGuid.assetGuid = v;
				}
			}
		}

		private void OnDisable()
		{
			foreach (Transform child in transform)
			{
				ReplicatedGuid guidToRemove = default;
				string         idToRemove   = default;
				foreach (var kvp in GuidToGameObject)
				{
					if (kvp.Value.LoadedAsset == child.gameObject)
					{
						guidToRemove = kvp.Key;
						if (kvp.Value.IsValid)
						{
							kvp.Value.SafeUnload();
						}

						break;
					}
				}

				foreach (var kvp in IdToGameObject)
				{
					if (kvp.Value.LoadedAsset == child.gameObject)
					{
						idToRemove = kvp.Key;
						if (kvp.Value.IsValid)
						{
							kvp.Value.SafeUnload();
						}

						break;
					}
				}

				if (idToRemove != null)
					IdToGameObject.Remove(idToRemove);
				if (!guidToRemove.Equals(default))
					GuidToGameObject.Remove(guidToRemove);
			}
		}
	}
}