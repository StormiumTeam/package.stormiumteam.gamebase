using UnityEngine;
using UnityEngine.AddressableAssets;

namespace StormiumTeam.GameBase
{
	public class LoadModelBehaviour : MonoBehaviour
	{
		public AssetReference Asset;

		private GameObject m_Result;
		public  Transform  SpawnRoot;

		private void OnEnable()
		{
			Asset.Instantiate(Vector3.zero, Quaternion.identity, SpawnRoot).Completed += o => m_Result = o.Result;
		}

		private void OnDisable()
		{
			if (m_Result)
				Asset.ReleaseInstance(m_Result);
		}
	}
}