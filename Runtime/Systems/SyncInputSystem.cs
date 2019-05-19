using UnityEngine.InputSystem;

namespace StormiumTeam.GameBase.Systems
{
	public abstract class SyncInputSystem : GameBaseSyncMessageSystem
	{
		public InputActionAsset Asset { get; private set; }

		protected bool Refresh(InputActionAsset asset)
		{
			Asset = asset;
			Asset.Enable();
			
			OnAssetRefresh();

			return Asset != null;
		}

		protected abstract void OnAssetRefresh();
	}
}