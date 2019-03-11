using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(GameObjectEntity))]
	public class SolidShape : MonoBehaviour
	{
		private      bool m_IsEnabled;
		public const int  HitLayer = 20;

		public bool IsEnabled => m_IsEnabled;

		void OnEnable()
		{
			Enable();
		}

		public void Enable()
		{
			if (m_IsEnabled)
				return;

			gameObject.layer = HitLayer;
			m_IsEnabled      = true;
		}

		public void Disable()
		{
			if (!m_IsEnabled)
				return;

			gameObject.layer = GameBaseConstants.NoCollision;
			m_IsEnabled      = false;
		}
	}

	public class SolidShapeUpdateSystem : GameBaseSystem, IOnQueryEnableCollisionFor
	{
		protected override void OnCreateManager()
		{
			base.OnCreateManager();
			
			World.GetExistingManager<AppEventSystem>().SubscribeToAll(this);
		}

		protected override void OnUpdate()
		{
		}

		public bool EnableCollisionFor(Entity entity)
		{
			return true;
		}

		public void EnableCollision()
		{
			ForEach((SolidShape solidShape) =>
			{
				solidShape.Enable();
			});
		}

		public void DisableCollision()
		{
			throw new System.NotImplementedException("This should never happen.");
		}
	}
}