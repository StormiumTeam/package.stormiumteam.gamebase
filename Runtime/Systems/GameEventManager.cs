using Unity.NetCode;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace StormiumTeam.GameBase
{
	public struct GameEvent : IComponentData
	{
		public uint SnapshotTick;
		public int  SimulationTick;
	}
}