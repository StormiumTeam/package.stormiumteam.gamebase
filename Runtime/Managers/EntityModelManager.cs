using System.Collections.Generic;
using StormiumTeam.Shared;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase
{
    public struct ModelIdent : IComponentData
    {
        public int Value;
    }
    
    public class EntityModelManager : ComponentSystem
    {
        private struct DValue
        {
            public SpawnEntityDelegate Spawn;
            public DestroyEntityDelegate Destroy;

            public ComponentType[] Components;
        }
        
        public delegate Entity SpawnEntityDelegate(Entity origin);
        public delegate void DestroyEntityDelegate(Entity worldEntity);

        private PatternManager m_PatternManager;

        private readonly Dictionary<int, DValue> m_ModelsData = new Dictionary<int, DValue>();

        private void Init()
        {
            m_PatternManager = World.GetOrCreateSystem<PatternManager>();
        }
        
        protected override void OnUpdate()
        {
            
        }

        public ModelIdent Register(string name, SpawnEntityDelegate spawn, DestroyEntityDelegate destroy)
        {
            var pattern = m_PatternManager.LocalBank.Register(new PatternIdent(name));
            
            m_ModelsData[pattern.Id] = new DValue
            {
                Spawn   = spawn,
                Destroy = destroy
            };

            return new ModelIdent {Value = pattern.Id};
        }

        public ModelIdent RegisterFull(string name, ComponentType[] components, SpawnEntityDelegate spawn, DestroyEntityDelegate destroy)
        {
            var pattern = m_PatternManager.LocalBank.Register(new PatternIdent(name));
            
            m_ModelsData[pattern.Id] = new DValue
            {
                Spawn   = spawn,
                Destroy = destroy,
                
                Components = components
            };

            return new ModelIdent {Value = pattern.Id};
        }

        public Entity SpawnEntity(int modelId, Entity origin)
        {
            #if UNITY_EDITOR || DEBUG
            if (!m_ModelsData.ContainsKey(modelId))
            {
                PatternResult pattern = default;
                try
                {
                    pattern = m_PatternManager.LocalBank.GetPatternResult(modelId);
                }
                catch
                {
                    // ignored
                }
                
                if (pattern.Id != modelId)
                    Debug.LogError($"Pattern ({pattern.Internal.Name}) isn't correct ({pattern.Id} != {modelId})");
                
                Debug.LogError($"No Spawn Callbacks found for modelId={modelId}");
            }    
            #endif

            var modelData = m_ModelsData[modelId];
            var entity = modelData.Spawn(origin);

            EntityManager.SetComponentData(entity, new ModelIdent {Value = modelId});
            
            return entity;
        }

        public void DestroyEntity(Entity worldEntity, int modelId)
        {
            var callbackObj = m_ModelsData[modelId].Destroy;
            if (callbackObj == null)
            {
                EntityManager.DestroyEntity(worldEntity);
                return;
            }

            callbackObj.Invoke(worldEntity);
        }
    }
}