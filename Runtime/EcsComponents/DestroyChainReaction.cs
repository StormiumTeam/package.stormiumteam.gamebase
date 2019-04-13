using Unity.Entities;
using UnityEngine.Experimental.PlayerLoop;

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
	
	[UpdateAfter(typeof(PreLateUpdate))]
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