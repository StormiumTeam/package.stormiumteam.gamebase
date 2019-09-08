using Revolution.NetCode;
using Unity.Entities;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	[UpdateBefore(typeof(GameModeSystemGroup))]
	public class ActionSystemGroup : ComponentSystemGroup
	{
	}
}