using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace StormiumTeam.GameBase
{
    public interface IOwnerDescription : IComponentData
    {
    }
    
    /// <summary>
    /// All entities that can be collided/hit/damaged (eg: character movable entity (not the character itself!))
    /// </summary>
    public struct ColliderDescription : IOwnerDescription
    {
        public class OwnerSync : OwnerStateSync<ColliderDescription>
        {}
    }

    /// <summary>
    /// All entities that can move (eg: character movable entity (not the character itself!))
    /// </summary>
    public struct MovableDescription : IOwnerDescription
    {
        public class OwnerSync : OwnerStateSync<MovableDescription>
        {}
    }
    
    /// <summary>
    /// All entities that are described as livables (eg: characters)
    /// </summary>
    public struct LivableDescription : IOwnerDescription
    {
        public class OwnerSync : OwnerStateSync<LivableDescription>
        {}
    }

    /// <summary>
    /// All entities that are described as characters
    /// </summary>
    public struct CharacterDescription : IOwnerDescription
    {
        public class OwnerSync : OwnerStateSync<CharacterDescription>
        {}
    }

    /// <summary>
    /// All entities that are described as players
    /// </summary>
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

            //void DirectAddOrSet(Entity entity, Entity owner);
            ComponentType ComponentType { get; }
        }

        public static void ReplaceOwnerData(this EntityManager entityManager, Entity source, Entity owner)
        {
            Debug.Log("replacing owner data...");

            // the name OwnerChildren can be confusing, but let's you have a LivableDesc with a MovableDesc and ColliderDesc.
            // if you want to get the collider from the movable, OwnerChild will be able to help with that.
            NativeArray<OwnerChild> ownerChildren = default;

            var hasBuffer = entityManager.HasComponent(owner, typeof(OwnerChild));
            if (hasBuffer)
                ownerChildren = new NativeArray<OwnerChild>(entityManager.GetBuffer<OwnerChild>(owner).AsNativeArray(), Allocator.Temp);

            foreach (var obj in AppEvent<ISyncEvent>.GetObjEvents())
            {
                for (var i = 0; hasBuffer && i != ownerChildren.Length; i++)
                {
                    if (obj.ComponentType.TypeIndex == ownerChildren[i].TypeId)
                        obj.SyncOwnerToEntity(source, ownerChildren[i].Child);
                }

                obj.SyncOwnerToEntity(source, owner);
            }
            
            if (hasBuffer)
                ownerChildren.Dispose();
        }

        public static void ReplaceOwnerData(this EntityCommandBuffer entityCommandBuffer, Entity source, Entity owner)
        {
            foreach (var obj in AppEvent<ISyncEvent>.GetObjEvents())
            {
                obj.SyncOwnerToEntity(entityCommandBuffer, source, owner);
            }
        }
    }

    public class OwnerStateSync<T> : JobComponentSystem, OwnerState.ISyncEvent
        where T : struct, IOwnerDescription
    {
        public ComponentType ComponentType => ComponentType.ReadWrite<T>();
        
        protected override void OnCreateManager()
        {
            World.GetOrCreateManager<AppEventSystem>().SubscribeToAll(this);
        }

        protected override JobHandle OnUpdate(JobHandle _)
        {
            return _;
        }

        public void SyncOwnerToEntity(Entity origin, Entity owner)
        {
            var ownerStates = GetComponentDataFromEntity<OwnerState<T>>();

            if (ownerStates.Exists(owner))
            {
                if (ownerStates.Exists(origin))
                    ownerStates[origin] = ownerStates[owner];
                else
                    EntityManager.AddComponentData(origin, ownerStates[owner]);

                // Debug.Log($"({GetType().FullName}) Hierarchy parent {owner} added to {origin}");
            }

            // resync...
            ownerStates = GetComponentDataFromEntity<OwnerState<T>>();
            var descriptions = GetComponentDataFromEntity<T>();
            if (descriptions.Exists(owner))
            {
                if (ownerStates.Exists(origin))
                    ownerStates[origin] = new OwnerState<T> {Target = owner};
                else
                    EntityManager.AddComponentData(origin, new OwnerState<T> {Target = owner});
            }

            // Debug.Log($"({GetType().FullName}) Owner {owner} added to {origin}");
        }

        public void SyncOwnerToEntity(EntityCommandBuffer entityCommandBuffer, Entity origin, Entity owner)
        {
            var ownerStates  = GetComponentDataFromEntity<OwnerState<T>>();
            var descriptions = GetComponentDataFromEntity<T>();

            if (ownerStates.Exists(owner))
            {
                if (ownerStates.Exists(origin))
                    ownerStates[origin] = ownerStates[owner];
                else
                    entityCommandBuffer.AddComponent(origin, ownerStates[owner]);
            }

            if (!descriptions.Exists(owner) || ownerStates.Exists(origin))
                return;

            // resync...
            ownerStates  = GetComponentDataFromEntity<OwnerState<T>>();
            descriptions = GetComponentDataFromEntity<T>();
            
            if (ownerStates.Exists(owner))
                ownerStates[owner] = new OwnerState<T> {Target = owner};
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

    [InternalBufferCapacity(8)]
    public struct OwnerChild : IBufferElementData
    {
        public Entity Child;
        public int TypeId;
        
        public bool Is<T>(T t)
        {
            return TypeId == ComponentType.ReadOnly<T>().TypeIndex;
        }

        public bool Is(ComponentType componentType)
        {
            return TypeId == componentType.TypeIndex;
        }

        public OwnerChild(Entity entity, ComponentType componentType)
        {
            Child = entity;
            TypeId = componentType.TypeIndex;
        }

        public static OwnerChild Create<T>(Entity entity)
            where T : IOwnerDescription
        {
            return new OwnerChild(entity, ComponentType.ReadOnly<T>());
        }
    }
}