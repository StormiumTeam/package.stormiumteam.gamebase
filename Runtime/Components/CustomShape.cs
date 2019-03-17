using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	[ExecuteInEditMode]
	public class CustomShape : MonoBehaviour
	{
		public const int HitLayer = 21;
		private bool m_IsEnabled;
		
		protected EntityManager EntityManager;
		
		private void Awake()
		{
			EntityManager = World.Active.GetExistingManager<EntityManager>();
			World.Active.GetExistingManager<AppEventSystem>().SubscribeToAll(this);
		}
		
		public void OnEnable()
		{
			Enable();
		}

		public void Enable()
		{
			if (m_IsEnabled)
				return;
			
			gameObject.layer = HitLayer;
			m_IsEnabled = true;
		}

		public void Disable()
		{
			if (!m_IsEnabled)
				return;
				
			gameObject.layer = GameBaseConstants.NoCollision;
			m_IsEnabled = false;
		}
	}
}