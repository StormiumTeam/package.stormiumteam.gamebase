using Revolution.NetCode;
using Unity.Entities;
using Unity.Physics.Systems;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	[UpdateAfter(typeof(ActionSystemGroup))]
	public class ProjectileSystemGroup : ComponentSystemGroup
	{
		private EndProjectileEntityCommandBufferSystem m_EndBarrier;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_EndBarrier = World.GetOrCreateSystem<EndProjectileEntityCommandBufferSystem>();
		}

		protected override void OnUpdate()
		{
			World.GetOrCreateSystem<BuildPhysicsWorld>().Update();

			base.OnUpdate();
			
			m_EndBarrier.Update();
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

	[DisableAutoCreation]
	public class EndProjectileEntityCommandBufferSystem : EntityCommandBufferSystem
	{
	}
}