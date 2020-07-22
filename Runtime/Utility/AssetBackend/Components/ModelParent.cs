using Unity.Entities;

namespace StormiumTeam.GameBase.Utility.AssetBackend.Components
{
	public struct ModelParent : IComponentData
	{
		public Entity Parent;
	}
}