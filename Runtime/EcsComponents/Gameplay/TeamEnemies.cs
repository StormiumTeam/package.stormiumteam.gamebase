using Unity.Entities;

namespace StormiumTeam.GameBase.Components
{
	public struct TeamEnemies : IBufferElementData
	{
		public Entity Target;
	}
}