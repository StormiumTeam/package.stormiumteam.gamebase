using Unity.Entities;

namespace StormiumTeam.GameBase.Components
{
	/// <summary>
	///     Indicate if the entity is used for gamePlay or not.
	/// </summary>
	/// <remarks>
	///     The principal use of this tag is to safely destroy GamePlay entities (instead of destroying entities that contains
	///     important information (eg: user data, game player))
	/// </remarks>
	public struct PlayEntityTag : IComponentData
	{
	}
}