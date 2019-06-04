using Unity.Entities;
using Unity.NetCode;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	public class ActionSystemGroup : ComponentSystemGroup
	{}
}	