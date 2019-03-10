using package.stormiumteam.shared;
using Stormium.Core;
using Unity.Entities;

namespace Runtime.Components
{
	public class CollisionForCharacter : CustomShape, IOnQueryEnableCollisionFor
	{
		public bool collide;

		private EntityManager m_EntityManager;
		
		private void Awake()
		{
			m_EntityManager = World.Active.GetExistingManager<EntityManager>();
			World.Active.GetExistingManager<AppEventSystem>().SubscribeToAll(this);
		}

		public bool EnableCollisionFor(Entity entity)
		{
			return collide || !m_EntityManager.HasComponent<CharacterDescription>(entity);
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