using Unity.Entities;

namespace StormiumTeam.GameBase
{
	public struct GameEvent : IComponentData
	{
		public UTick Tick;
	}
}