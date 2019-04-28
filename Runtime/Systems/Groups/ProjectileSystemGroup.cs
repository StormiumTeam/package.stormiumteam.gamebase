using Unity.Entities;
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
	}
}