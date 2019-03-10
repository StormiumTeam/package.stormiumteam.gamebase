using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Runtime
{
    public struct ModelParent : IComponentData
    {
        public Entity Parent;
    }
    
    public abstract class OnModelLoadedListener : MonoBehaviour
    {
        public abstract void React(Entity parentEntity, EntityManager entityManager, GameObject parentGameObject);
    }
    
    public class LoadModelFromStringBehaviour : MonoBehaviour
    {
        private string    m_AssetId;

        private EntityManager m_EntityManager;
        private Entity m_EntityToSubModel;
        
        public Transform SpawnRoot;

        public string AssetId
        {
            get => m_AssetId;
            set
            {
                if (m_AssetId == value)
                    return;

                m_AssetId = value;
                Pop();
            }
        }

        private GameObject m_Result;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(m_AssetId))
            {
                return;
            }

            Pop();
        }

        private void Pop()
        {
            Depop();
            
            Addressables.Instantiate(m_AssetId, SpawnRoot).Completed += (o) =>
            {
                m_Result = o.Result;

                if (m_EntityManager == null)
                    return;
                    
                var gameObjectEntity = m_Result.GetComponent<GameObjectEntity>();
                if (!gameObjectEntity)
                {
                    gameObjectEntity = m_Result.AddComponent<GameObjectEntity>();
                }
                
                m_EntityManager.SetOrAddComponentData(m_EntityToSubModel, new SubModel(gameObjectEntity.Entity));
                m_EntityManager.AddComponentData(gameObjectEntity.Entity, new ModelParent{Parent = m_EntityToSubModel});

                var listeners = m_Result.GetComponents<OnModelLoadedListener>();
                foreach (var listener in listeners)
                {
                    listener.React(m_EntityToSubModel, m_EntityManager, m_Result);
                }
            };
        }

        private void Depop()
        {
            if (m_Result)
                Addressables.ReleaseInstance(m_Result);

            m_Result = null;
        }

        private void OnDisable()
        {
            Depop();
        }

        public void OnLoadSetSubModelFor(EntityManager entityManager, Entity entity)
        {
            m_EntityManager = entityManager;
            m_EntityToSubModel = entity;
        }
    }
}