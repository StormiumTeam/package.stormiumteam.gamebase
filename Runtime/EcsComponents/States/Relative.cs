using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace StormiumTeam.GameBase
{
    public interface IEntityDescription : IComponentData
    {
    }

    /// <summary>
    /// All entities that can be collided/hit/damaged (eg: character movable entity (not the character itself!))
    /// </summary>
    public struct ColliderDescription : IEntityDescription
    {
        public class OwnerSync : RelativeSync<ColliderDescription>
        {
        }
    }

    /// <summary>
    /// All entities that can move (eg: character movable entity (not the character itself!))
    /// </summary>
    public struct MovableDescription : IEntityDescription
    {
        public class OwnerSync : RelativeSync<MovableDescription>
        {
        }
    }

    /// <summary>
    /// All entities that are described as livables (eg: characters)
    /// </summary>
    public struct LivableDescription : IEntityDescription
    {
        public class OwnerSync : RelativeSync<LivableDescription>
        {
        }
    }

    /// <summary>
    /// All entities that are described as characters
    /// </summary>
    public struct CharacterDescription : IEntityDescription
    {
        public class OwnerSync : RelativeSync<CharacterDescription>
        {
        }
    }

    /// <summary>
    /// All entities that are described as players
    /// </summary>
    public struct PlayerDescription : IEntityDescription
    {
        public class OwnerSync : RelativeSync<PlayerDescription>
        {
        }
    }

    public struct ActionDescription : IEntityDescription
    {
        public class OwnerSync : RelativeSync<ActionDescription>
        {
        }
    }

    public struct ProjectileDescription : IEntityDescription
    {
        public class OwnerSync : RelativeSync<ProjectileDescription>
        {
        }
    }

    public struct Owner : IComponentData
    {
        public Entity Target;
    }

    /// <summary>
    /// Represent the ghostId, you shouldn't use this component directly as it will be converted to Owner later
    /// </summary>
    public struct GhostOwner : IComponentData
    {
        public int GhostId;
    }

    public static class Relative
    {
        public interface ISyncEvent : IAppEvent
        {
            void SyncRelativeToEntity(Entity              origin,              Entity owner);
            void SyncRelativeToEntity(EntityCommandBuffer entityCommandBuffer, Entity origin, Entity owner);
            void AddChildren(Entity                       origin,              Entity owner);

            //void DirectAddOrSet(Entity entity, Entity owner);
            ComponentType ComponentType { get; }
        }

        public static void AddChildrenOwner(this EntityManager entityManager, Entity source, Entity owner)
        {
            foreach (var obj in AppEvent<ISyncEvent>.GetObjEvents())
            {
                obj.AddChildren(source, owner);
            }
        }

        public static void ReplaceOwnerData(this EntityManager entityManager, Entity entity, Entity owner, bool setRelative = true, bool autoEntityLink = true)
        {
            entityManager.SetOrAddComponentData(entity, new Owner {Target = owner});

            if (!setRelative)
                return;

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
                        obj.SyncRelativeToEntity(entity, ownerChildren[i].Child);
                }

                obj.SyncRelativeToEntity(entity, owner);
            }

            if (hasBuffer)
                ownerChildren.Dispose();

            if (autoEntityLink)
                entityManager.SetOrAddComponentData(entity, new DestroyChainReaction(owner));
        }

        public static void ReplaceOwnerData(this EntityCommandBuffer entityCommandBuffer, Entity source, Entity owner)
        {
            foreach (var obj in AppEvent<ISyncEvent>.GetObjEvents())
            {
                obj.SyncRelativeToEntity(entityCommandBuffer, source, owner);
            }
        }
    }

    public class RelativeGroup : ComponentSystemGroup
    {
        protected override void OnUpdate()
        {
        }
    }

    [UpdateInGroup(typeof(RelativeGroup))]
    public class RelativeSync<T> : JobComponentSystem, Relative.ISyncEvent
        where T : struct, IEntityDescription
    {
        public ComponentType ComponentType => ComponentType.ReadWrite<T>();

        protected override void OnCreate()
        {
            World.GetOrCreateSystem<AppEventSystem>().SubscribeToAll(this);
        }

        protected override JobHandle OnUpdate(JobHandle _)
        {
            return _;
        }

        public void AddChildren(Entity origin, Entity owner)
        {
            var descriptions = GetComponentDataFromEntity<T>();
            if (descriptions.Exists(origin))
            {
                var buffer = EntityManager.GetBuffer<OwnerChild>(owner);
                for (var i = 0; i != buffer.Length; i++)
                {
                    if (buffer[i].Child == origin)
                        return;
                }

                buffer.Add(new OwnerChild(origin, ComponentType.ReadWrite<T>()));
            }
        }

        public void SyncRelativeToEntity(Entity origin, Entity owner)
        {
            var relative = GetComponentDataFromEntity<Relative<T>>();

            if (relative.Exists(owner))
            {
                if (relative.Exists(origin))
                    relative[origin] = relative[owner];
                else
                    EntityManager.AddComponentData(origin, relative[owner]);

                // Debug.Log($"({GetType().FullName}) Hierarchy parent {owner} added to {origin}");
            }

            // resync...
            relative = GetComponentDataFromEntity<Relative<T>>();
            var descriptions = GetComponentDataFromEntity<T>();
            if (descriptions.Exists(owner))
            {
                if (relative.Exists(origin))
                    relative[origin] = new Relative<T> {Target = owner};
                else
                    EntityManager.AddComponentData(origin, new Relative<T> {Target = owner});
            }

            // Debug.Log($"({GetType().FullName}) Owner {owner} added to {origin}");
        }

        public void SyncRelativeToEntity(EntityCommandBuffer entityCommandBuffer, Entity origin, Entity owner)
        {
            var relative     = GetComponentDataFromEntity<Relative<T>>();
            var descriptions = GetComponentDataFromEntity<T>();

            if (relative.Exists(owner))
            {
                if (relative.Exists(origin))
                    relative[origin] = relative[owner];
                else
                    entityCommandBuffer.AddComponent(origin, relative[owner]);
            }

            if (!descriptions.Exists(owner) || relative.Exists(origin))
                return;

            // resync...
            relative     = GetComponentDataFromEntity<Relative<T>>();
            descriptions = GetComponentDataFromEntity<T>();

            if (relative.Exists(owner))
                relative[owner] = new Relative<T> {Target = owner};
            else
                entityCommandBuffer.AddComponent(origin, new Relative<T> {Target = owner});
        }
    }

    // Todo: find a way to synchronize it nicely.
    public struct Relative<TDescription> : IComponentData
        where TDescription : struct, IEntityDescription
    {
        public Entity Target;
    }

    [InternalBufferCapacity(8)]
    public struct OwnerChild : IBufferElementData
    {
        public Entity Child;
        public int    TypeId;

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
            Child  = entity;
            TypeId = componentType.TypeIndex;
        }

        public static OwnerChild Create<T>(Entity entity)
            where T : IEntityDescription
        {
            return new OwnerChild(entity, ComponentType.ReadOnly<T>());
        }
    }
}