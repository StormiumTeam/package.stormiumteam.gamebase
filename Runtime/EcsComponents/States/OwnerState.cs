using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace StormiumTeam.GameBase
{
    public interface IOwnerDescription : IComponentData
    {
    }

    public struct LivableDescription : IOwnerDescription
    {
        public class OwnerSync : OwnerStateSync<LivableDescription>
        {}
    }

    public struct CharacterDescription : IOwnerDescription
    {
        public class OwnerSync : OwnerStateSync<CharacterDescription>
        {}
    }

    public struct PlayerDescription : IOwnerDescription
    {
        public class OwnerSync : OwnerStateSync<PlayerDescription>
        {}
    }

    public struct ActionDescription : IOwnerDescription
    {
        public class OwnerSync : OwnerStateSync<ActionDescription>
        {}
    }

    public struct ProjectileDescription : IOwnerDescription
    {
        public class OwnerSync : OwnerStateSync<ProjectileDescription>
        {}
    }

    public static class OwnerState
    {
        public interface ISyncEvent : IAppEvent
        {
            void SyncOwnerToEntity(Entity origin, Entity owner);
            void SyncOwnerToEntity(EntityCommandBuffer entityCommandBuffer, Entity origin, Entity owner);
        }
        
        public static void ReplaceOwnerData(this EntityManager entityManager, Entity source, Entity owner)
        {
            Debug.Log("replacing owner data...");
            foreach (var obj in AppEvent<ISyncEvent>.GetObjEvents())
            {
                Debug.Log(obj);
                obj.SyncOwnerToEntity(source, owner);
            }
        }
        
        public static void ReplaceOwnerData(this EntityCommandBuffer entityCommandBuffer, Entity source, Entity owner)
        {
            foreach (var obj in AppEvent<ISyncEvent>.GetObjEvents())
            {
                obj.SyncOwnerToEntity(entityCommandBuffer, source, owner);
            }
        }
    }

    public class OwnerStateSync<T> : ComponentSystem, OwnerState.ISyncEvent
        where T : struct, IOwnerDescription
    {
        private ComponentDataFromEntity<OwnerState<T>> m_OwnerStates; 
        private ComponentDataFromEntity<T> m_Descriptions; 
        
        protected override void OnCreateManager()
        {
            World.GetOrCreateManager<AppEventSystem>().SubscribeToAll(this);
        }

        protected override void OnUpdate()
        {
        }

        public void SyncOwnerToEntity(Entity origin, Entity owner)
        {
            m_OwnerStates = GetComponentDataFromEntity<OwnerState<T>>();

            if (m_OwnerStates.Exists(owner))
            {
                if (m_OwnerStates.Exists(origin))
                    m_OwnerStates[origin] = m_OwnerStates[owner];
                else
                    EntityManager.AddComponentData(origin, m_OwnerStates[owner]);

                // Debug.Log($"({GetType().FullName}) Hierarchy parent {owner} added to {origin}");
            }

            m_Descriptions = GetComponentDataFromEntity<T>();
            if (!m_Descriptions.Exists(owner))
                return;

            if (m_OwnerStates.Exists(origin))
                m_OwnerStates[origin] = new OwnerState<T> {Target = owner};
            else
                EntityManager.AddComponentData(origin, new OwnerState<T> {Target = owner});

            // Debug.Log($"({GetType().FullName}) Owner {owner} added to {origin}");
        }

        public void SyncOwnerToEntity(EntityCommandBuffer entityCommandBuffer, Entity origin, Entity owner)
        {
            m_OwnerStates  = GetComponentDataFromEntity<OwnerState<T>>();
            m_Descriptions = GetComponentDataFromEntity<T>();

            if (m_OwnerStates.Exists(owner))
            {
                if (m_OwnerStates.Exists(origin))
                    m_OwnerStates[origin] = m_OwnerStates[owner];
                else
                    entityCommandBuffer.AddComponent(origin, m_OwnerStates[owner]);
            }

            if (!m_Descriptions.Exists(owner) || m_OwnerStates.Exists(origin))
                return;
            
            if (m_OwnerStates.Exists(owner))
                m_OwnerStates[owner] = new OwnerState<T> {Target = owner};
            else
                entityCommandBuffer.AddComponent(origin, new OwnerState<T> {Target = owner});
        }
    }

    public struct OwnerState<TOwnerDescription> : IStateData, IComponentData, ISerializableAsPayload
        where TOwnerDescription : struct, IOwnerDescription
    {
        public Entity Target;

        public void Write(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime runtime)
        {
            data.WriteValue(Target);
        }

        public void Read(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
        {
            Target = runtime.EntityToWorld(data.ReadValue<Entity>());
        }

        public class Streamer : SnapshotEntityDataManualValueTypeStreamer<OwnerState<TOwnerDescription>>
        {
        }
    }
}