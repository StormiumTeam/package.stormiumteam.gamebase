using GameBase.Roles.Components;
using GameBase.Roles.Descriptions;
using GameBase.Roles.Interfaces;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<PlayerDescription>))]

namespace GameBase.Roles.Descriptions
{
	public struct PlayerDescription : IEntityDescription
	{
		public class RegisterRelative : Relative<PlayerDescription>.Register
		{
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