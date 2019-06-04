using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Physics.Systems;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	[UpdateAfter(typeof(ActionSystemGroup))]
	public class ProjectileSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
			World.GetExistingSystem<BuildPhysicsWorld>().Update();

			base.OnUpdate();
		}
	}

	[UpdateInGroup(typeof(ProjectileSystemGroup))]
	public class ProjectilePhysicIterationSystemGroup : ComponentSystemGroup
	{
	}

	[UpdateInGroup(typeof(ProjectileSystemGroup)), UpdateAfter(typeof(ProjectilePhysicIterationSystemGroup))]
	public class ProjectilePhysicCollisionEventSystemGroup : ComponentSystemGroup
	{
	}
}