using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Roles.Interfaces;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<PlayerDescription>))]

namespace StormiumTeam.GameBase.Roles.Descriptions
{
	public struct PlayerDescription : IEntityDescription
	{
		public class RegisterRelative : Relative<PlayerDescription>.Register
		{
			public override ICustomComponentDeserializer BurstKnowDeserializer()
			{
				return new CustomSingleDeserializer<Relative<PlayerDescription>, Relative<PlayerDescription>.ValueDeserializer>();
			}
		}

		public class Register : RegisterGameHostComponentData<PlayerDescription>
		{
		}
	}

	/// <summary>
	///     Indicate whether or not this player is local
	/// </summary>
	public struct PlayerIsLocal : IComponentData
	{
		public class Register : RegisterGameHostComponentData<PlayerIsLocal>
		{
		}
	}
}