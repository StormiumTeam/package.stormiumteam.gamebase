using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Roles.Interfaces;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<TeamDescription>))]

namespace StormiumTeam.GameBase.Roles.Descriptions
{
	public struct TeamDescription : IEntityDescription
	{
		public class RegisterRelative : Relative<TeamDescription>.Register
		{
			public override ICustomComponentDeserializer BurstKnowDeserializer()
			{
				return new CustomSingleDeserializer<Relative<TeamDescription>, Relative<TeamDescription>.ValueDeserializer>();
			}
		}

		public class Register : RegisterGameHostComponentData<TeamDescription>
		{
		}
	}
}