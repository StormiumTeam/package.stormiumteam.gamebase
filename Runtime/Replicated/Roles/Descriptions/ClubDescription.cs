using GameHost.Native;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Roles.Interfaces;
using Unity.Entities;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(Relative<ClubDescription>))]

namespace StormiumTeam.GameBase.Roles.Descriptions
{
	public struct ClubDescription : IEntityDescription
	{
		public class RegisterRelative : Relative<ClubDescription>.Register
		{
			public override ICustomComponentDeserializer BurstKnowDeserializer()
			{
				return new CustomSingleDeserializer<Relative<ClubDescription>, Relative<ClubDescription>.ValueDeserializer>();
			}
		}

		public class Register : RegisterGameHostComponentData<ClubDescription>
		{
		}
	}

	public struct ClubInformation : IComponentData
	{
		public CharBuffer64 Name;
		public Color32      PrimaryColor;
		public Color32      SecondaryColor;
		
		public class Register : RegisterGameHostComponentData<ClubInformation>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<ClubInformation>();
		}
	}
}