using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace StormiumTeam.GameBase.GamePlay.Projectiles
{
	public readonly struct ProjectileEndedTag : IComponentData
	{
		public class Register : RegisterGameHostComponentData<ProjectileEndedTag>
		{}
	}

	public readonly struct ProjectileExplodedEndReason : IComponentData
	{
		public class Register : RegisterGameHostComponentData<ProjectileExplodedEndReason>
		{}
	}

	public readonly struct ProjectileOutOfTimeEndReason : IComponentData
	{
		public class Register : RegisterGameHostComponentData<ProjectileOutOfTimeEndReason>
		{}
	}
}