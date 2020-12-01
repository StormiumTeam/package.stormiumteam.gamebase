using Unity.Entities;

namespace GameHost
{
	public struct ReplicatedGameEntity : IComponentData
	{
		public GhGameEntitySafe Source;
		public uint             ArchetypeId;
	}
}