using Unity.Entities;
using UnityEngine;

namespace Runtime
{
    public class DestroyGameObjectOnEntityDestroyed : MonoBehaviour
    {
        private GameObjectEntity m_GameObjectEntity;

        private void OnEnable()
        {
            m_GameObjectEntity = GetComponent<GameObjectEntity>();
        }

        private void Update()
        {
            var entity = m_GameObjectEntity.Entity;
            var entityMgr = m_GameObjectEntity.EntityManager;
            
            if (entity == default || !entityMgr.Exists(entity))
                Destroy(gameObject);
        }

        private void OnDisable()
        {
            m_GameObjectEntity = null;
        }
    }
}