using Unity.Entities;

namespace StormiumTeam.GameBase.GameHost.Simulation
{
	[UpdateAfter(typeof(Core.ENet.InitializeLibrarySystem))]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public class ReceiveGameHostSystemGroup : ComponentSystemGroup
	{
	}
}