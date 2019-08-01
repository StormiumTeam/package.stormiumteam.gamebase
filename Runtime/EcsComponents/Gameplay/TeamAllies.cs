using Unity.Entities;

namespace StormiumTeam.GameBase.Components
{
	public struct TeamAllies : IBufferElementData
	{
		public Entity Target;
	}
}