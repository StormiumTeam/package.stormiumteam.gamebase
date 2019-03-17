using package.stormiumteam.shared;
using Unity.Entities;

namespace StormiumTeam.GameBase.Components
{
	public class CollisionForCharacter : CustomShape, IOnQueryEnableCollisionFor
	{
		public bool collide;

		public bool EnableCollisionFor(Entity entity)
		{
			return collide || !EntityManager.HasComponent<CharacterDescription>(entity);
		}

		public void EnableCollision()
		{
			Enable();
		}

		public void DisableCollision()
		{
			Disable();
		}
	}
}