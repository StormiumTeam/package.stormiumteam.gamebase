using Unity.Entities;

namespace Runtime.EcsComponents
{
	/// <summary>
	/// Should only be present in Server world. Used to link between GamePlayers and connection entities
	/// </summary>
	public struct NetworkOwner : IComponentData
	{
		public Entity Value;
	}
}