using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace StormiumTeam.GameBase.Roles.Components
{
	/// <summary>
	/// Is this entity simulated by us?
	/// </summary>
	public struct IsSimulationOwned : IComponentData
	{
		public class Register : RegisterGameHostComponentData<IsSimulationOwned>
		{}
	}
}