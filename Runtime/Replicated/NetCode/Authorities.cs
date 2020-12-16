using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace StormiumTeam.GameBase.Network.Authorities
{
	/// <summary>
	/// This entity can simulate itself
	/// </summary>
	public struct SimulationAuthority : IComponentData
	{
		public class Register : RegisterGameHostComponentData<SimulationAuthority>
		{}
	}
	
	/// <summary>
	/// This entity can modify its inputs
	/// </summary>
	public struct InputAuthority : IComponentData
	{
		public class Register : RegisterGameHostComponentData<InputAuthority>
		{}
	}
}