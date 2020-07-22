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
		public class RegisterRelative : RegisterGameHostComponentSystemBase<Relative<PlayerDescription>>
		{
		}
	}
}