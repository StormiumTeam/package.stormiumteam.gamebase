using Unity.Entities;
using Unity.NetCode;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	[UpdateBefore(typeof(GameModeSystemGroup))]
	public class ActionSystemGroup : BaseGhostPredictionSystemGroup
	{
	}
}