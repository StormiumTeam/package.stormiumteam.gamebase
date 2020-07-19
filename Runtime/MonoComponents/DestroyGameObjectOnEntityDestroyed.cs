using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public class DestroyGameObjectOnEntityDestroyed : MonoBehaviour
	{
		private Entity        m_TargetEntity;
		private EntityManager m_TargetEntityManager;

		private void OnEnable()
		{
			var gameObjectEntity = GetComponent<GameObjectEntity>();
			if (!gameObjectEntity)
				return;

			m_TargetEntityManager = gameObjectEntity.EntityManager;
			m_TargetEntity        = gameObjectEntity.Entity;
		}

		private void Update()
		{
			if (m_TargetEntity == default || m_TargetEntityManager.World == null || !m_TargetEntityManager.Exists(m_TargetEntity))
				Destroy(gameObject);
		}

		private void OnDisable()
		{
			m_TargetEntityManager = null;
		}

		public void SetTarget(EntityManager entityManager, Entity entity)
		{
			m_TargetEntityManager = entityManager;
			m_TargetEntity        = entity;
		}
	}
}