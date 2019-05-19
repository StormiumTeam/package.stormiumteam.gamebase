using Unity.Entities;

namespace StormiumTeam.GameBase.Data
{
	public struct DestroyChainReaction : IComponentData
	{
		public Entity Target;

		public DestroyChainReaction(Entity target)
		{
			Target = target;
		}
	}
	
	public class DestroyChainReactionSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((Entity entity, ref DestroyChainReaction destroyChainReaction) =>
			{
				if (!EntityManager.Exists(destroyChainReaction.Target))
					PostUpdateCommands.DestroyEntity(entity);
			});
		}
	}
}