using Revolution.NetCode;
using Unity.Entities;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	[UpdateBefore(typeof(GameModeSystemGroup))]
	public class ActionSystemGroup : ComponentSystemGroup
	{
	}
}