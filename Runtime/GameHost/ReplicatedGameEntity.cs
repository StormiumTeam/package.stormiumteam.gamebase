using Unity.Entities;

namespace GameHost
{
	public struct ReplicatedGameEntity : IComponentData
	{
		public GhGameEntity Source;
		public uint         ArchetypeId;
	}
}