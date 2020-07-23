using Unity.Entities;

namespace StormiumTeam.GameBase.Utility.AssetBackend.Components
{
	public struct RuntimeAssetDisable : IComponentData
	{
		public static RuntimeAssetDisable All =>
			new RuntimeAssetDisable
			{
				IgnoreParent       = false,
				DisableGameObject  = true,
				ReturnToPool       = true,
				ReturnPresentation = true
			};

		public static RuntimeAssetDisable AllAndIgnoreParent =>
			new RuntimeAssetDisable
			{
				IgnoreParent       = true,
				DisableGameObject  = true,
				ReturnToPool       = true,
				ReturnPresentation = true
			};

		public bool IgnoreParent;
		public bool DisableGameObject;
		public bool ReturnToPool;
		public bool ReturnPresentation;
	}
}