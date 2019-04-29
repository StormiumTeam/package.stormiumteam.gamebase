using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;

namespace StormiumTeam.GameBase
{
	[UpdateAfter(typeof(ActionSystemGroup))]
	public class ProjectileSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
			World.GetExistingSystem<BuildPhysicsWorld>().Update();

			base.OnUpdate();
		}

		public override void SortSystemUpdateList()
		{
			base.SortSystemUpdateList();
		}
	}

	[UpdateInGroup(typeof(ProjectileSystemGroup))]
	public class ProjectilePhysicIterationSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
			JobHandle handle = default;

			foreach (var system in m_systemsToUpdate)
			{
				if (system is GameBaseSystem gameSystem)
				{
					gameSystem.SystemGroup_CanHaveDependency(true);
					gameSystem.SetDependency(handle);
					gameSystem.Update();

					handle = gameSystem.GetDependency();
				}
				else
				{
					handle.Complete();
				}
			}

			handle.Complete();
		}
	}

	[UpdateInGroup(typeof(ProjectileSystemGroup)), UpdateAfter(typeof(ProjectilePhysicIterationSystemGroup))]
	public class ProjectilePhysicCollisionEventSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
			JobHandle handle = default;

			foreach (var system in m_systemsToUpdate)
			{
				var gameSystem = system as GameBaseSystem;

				gameSystem?.SystemGroup_CanHaveDependency(true);
				gameSystem?.SetDependency(handle);
			}

			base.OnUpdate();

			handle.Complete();
		}
	}
}