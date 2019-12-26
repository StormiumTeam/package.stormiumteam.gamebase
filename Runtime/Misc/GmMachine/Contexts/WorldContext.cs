using GmMachine;
using Unity.Entities;

namespace Misc.GmMachine.Contexts
{
	public class WorldContext : ExternalContextBase
	{
		public readonly EntityManager EntityMgr;
		public readonly World         World;

		public WorldContext(World world)
		{
			World     = world;
			EntityMgr = world.EntityManager;
		}

		public T GetOrCreateSystem<T>() where T : ComponentSystemBase
		{
			return World.GetOrCreateSystem<T>();
		}

		public T GetExistingSystem<T>() where T : ComponentSystemBase
		{
			return World.GetExistingSystem<T>();
		}
	}
}