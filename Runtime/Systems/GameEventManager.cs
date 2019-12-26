using Unity.Entities;

namespace StormiumTeam.GameBase
{
	public struct GameEvent : IComponentData
	{
		public uint SnapshotTick;
		public int  SimulationTick;
	}
}