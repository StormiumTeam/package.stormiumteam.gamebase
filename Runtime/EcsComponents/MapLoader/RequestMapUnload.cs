using Unity.Entities;

namespace StormiumTeam.GameBase.Data
{
	/// <summary>
	///     Request to unload a map
	/// </summary>
	/// <example>
	///     If you want to set a map from a scene that is currently loaded
	///     this event may be useful.
	///     var unload = EntityManager.CreateEntity(typeof(RequestMapUnload));
	///     var setMapData = EntityManager.CreateEntity(typeof(ForceMapData));
	///     {
	///     EntityManager.SetComponentData(setMapData, new ForceMapData
	///     {
	///     NewKey = mapKey
	///     });
	///     }
	/// </example>
	public struct RequestMapUnload : IComponentData
	{
	}
}