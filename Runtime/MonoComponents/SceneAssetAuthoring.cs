using Revolution.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MonoComponents
{
	public class SceneAssetAuthoring : MonoBehaviour
	{
		public AssetReference addressableAsset;
		public string         addressableKey;
		public ReplicatedGuid assetGuid;
		public string         id;

		public object GetValidAddressableKey()
		{
			return addressableAsset.RuntimeKeyIsValid()
				? addressableAsset.RuntimeKey
				: string.IsNullOrEmpty(addressableKey)
					? null
					: addressableKey;
		}
	}
}