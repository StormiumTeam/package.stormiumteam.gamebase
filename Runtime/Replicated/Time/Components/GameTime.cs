using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace StormiumTeam.GameBase.Time.Components
{
	/// <summary>
	///     Represent the current time data of a <see cref="GameWorld" />
	/// </summary>
	public struct GameTime : IComponentData
	{
		public int    Frame;
		public float  Delta;
		public double Elapsed;

		public class Register : RegisterGameHostComponentData<GameTime>
		{
		}
	}
}