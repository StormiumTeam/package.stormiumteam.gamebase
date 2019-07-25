using Unity.Entities;
using Unity.NetCode;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class ActionSystemGroup : ComponentSystemGroup
	{}
}	