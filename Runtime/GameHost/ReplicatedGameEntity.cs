using Unity.Entities;

namespace StormiumTeam.GameBase.GameHost.Simulation
{
	public struct ReplicatedGameEntity : IComponentData
	{
		public GhGameEntity Source;
		public uint         ArchetypeId;
	}
}