using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace StormiumTeam.GameBase.GamePlay.Health
{
	/// <summary>
	/// Represent the health of a livable.
	/// </summary>
	public struct LivableHealth : IComponentData
	{
		public int Value, Max;

		public class Register : RegisterGameHostComponentData<LivableHealth>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<LivableHealth>();
		}
	}

	/// <summary>
	/// A tag that represent a dead livable
	/// </summary>
	public struct LivableIsDead : IComponentData
	{
		public class Register : RegisterGameHostComponentData<LivableIsDead>
		{

		}
	}
}