using Unity.Entities;

namespace GameHost
{
	[UpdateAfter(typeof(global::Core.ENet.InitializeLibrarySystem))]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public class ReceiveGameHostSystemGroup : ComponentSystemGroup
	{
	}
}