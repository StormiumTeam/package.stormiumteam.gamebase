using Unity.Entities;

namespace StormiumTeam.GameBase.Data
{
	public struct AsyncMapOperation : IComponentData
	{}
	
	/// <summary>
	/// Indicate if the game is currently loading a map.
	/// It's recommended to search an entity with this component before loading a map.
	/// </summary>
	public struct OperationMapLoadTag : IComponentData
	{
	}

	public struct OperationMapUnloadTag : IComponentData
	{
	}
}