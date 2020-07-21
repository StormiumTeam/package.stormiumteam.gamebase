using Core.ENet;
using Unity.Entities;

namespace GameHost
{
	[UpdateAfter(typeof(InitializeLibrarySystem))]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public class ReceiveGameHostSystemGroup : ComponentSystemGroup
	{
	}
}