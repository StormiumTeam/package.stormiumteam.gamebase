using Unity.Entities;
using Unity.Physics.Systems;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	[UpdateAfter(typeof(ActionSystemGroup))]
	public class ProjectileSystemGroup : ComponentSystemGroup
	{
		private BeginProjectileEntityCommandBufferSystem m_BeginBarrier;
		private EndProjectileEntityCommandBufferSystem   m_EndBarrier;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_BeginBarrier = World.GetOrCreateSystem<BeginProjectileEntityCommandBufferSystem>();
			m_EndBarrier   = World.GetOrCreateSystem<EndProjectileEntityCommandBufferSystem>();
		}

		protected override void OnUpdate()
		{
			m_BeginBarrier.Update();

			base.OnUpdate();

			m_EndBarrier.Update();
		}
	}

	[UpdateInGroup(typeof(ProjectileSystemGroup))]
	public class ProjectilePhysicIterationSystemGroup : ComponentSystemGroup
	{
	}

	[UpdateInGroup(typeof(ProjectileSystemGroup))] 
	[UpdateAfter(typeof(ProjectilePhysicIterationSystemGroup))]
	public class ProjectilePhysicCollisionEventSystemGroup : ComponentSystemGroup
	{
	}

	[DisableAutoCreation]
	public class BeginProjectileEntityCommandBufferSystem : EntityCommandBufferSystem
	{
	}

	[DisableAutoCreation]
	public class EndProjectileEntityCommandBufferSystem : EntityCommandBufferSystem
	{
	}
}