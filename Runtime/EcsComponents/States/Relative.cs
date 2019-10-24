using System.Linq;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using Revolution;
using Revolution.NetCode;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(Relative<HitShapeDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<MovableDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<LivableDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<CharacterDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<PlayerDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<ActionDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<ProjectileDescription>))]

namespace StormiumTeam.GameBase
{
    public interface IEntityDescription : IComponentData
    {
    }
    
    public struct ExcludeRelativeFromSnapshot : IComponentData
    {}

    public abstract class RelativeSynchronize<TDesc> : MixedComponentSnapshotSystem<Relative<TDesc>, GhostSetup>
        where TDesc : struct, IEntityDescription
    {
        public override ComponentType ExcludeComponent => typeof(ExcludeRelativeFromSnapshot);
    }

    /// <summary>
    /// All entities that can be hit/damaged (eg: character hitshape entities (not the character itself!))
    /// </summary>
    public struct HitShapeDescription : IEntityDescription
    {
        public class Sync : RelativeSynchronize<HitShapeDescription>
        {}
    }

    /// <summary>
    /// All entities that can move (eg: character movable entity (not the character itself!))
    /// </summary>
    public struct MovableDescription : IEntityDescription
    {
        public class Sync : RelativeSynchronize<MovableDescription>
        {}
    }

    /// <summary>
    /// All entities that are described as livables (eg: characters)
    /// </summary>
    public struct LivableDescription : IEntityDescription
    {
        public class Sync : RelativeSynchronize<LivableDescription>
        {}
    }

    /// <summary>
    /// All entities that are described as characters
    /// </summary>
    public struct CharacterDescription : IEntityDescription
    {
        public class Sync : RelativeSynchronize<CharacterDescription>
        {}
    }

    /// <summary>
    /// All entities that are described as players
    /// </summary>
    public struct PlayerDescription : IEntityDescription
    {
        public class Sync : RelativeSynchronize<PlayerDescription>
        {}
    }

    public struct ActionDescription : IEntityDescription
    {
        public class Sync : RelativeSynchronize<ActionDescription>
        {}
    }

    public struct ProjectileDescription : IEntityDescription
    {
        public class Sync : RelativeSynchronize<ProjectileDescription>
        {}
    }

    public struct Owner : IReadWriteComponentSnapshot<Owner, GhostSetup>
    {
        public struct Exclude : IComponentData
        {}
        
        private uint m_InternalPreviousGhostId;

        public Entity Target;

        public void WriteTo(DataStreamWriter writer, ref Owner baseline, GhostSetup setup, SerializeClientData jobData)
        {
            writer.WritePackedUIntDelta(setup[Target], setup[baseline.Target], jobData.NetworkCompressionModel);
        }

        public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Owner baseline, DeserializeClientData jobData)
        {
            var ghostId = reader.ReadPackedUIntDelta(ref ctx, m_InternalPreviousGhostId, jobData.NetworkCompressionModel);
            jobData.GhostToEntityMap.TryGetValue(ghostId, out Target);
            m_InternalPreviousGhostId = ghostId;
        }

        public class Sync : MixedComponentSnapshotSystem<Owner, GhostSetup>
        {
            public override ComponentType ExcludeComponent => typeof(Exclude);
        }
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

        public static void ReplaceOwnerData(this EntityManager entityManager, Entity entity, Entity owner, bool setRelative = true, bool setChildrenRelative = true, bool createChildBuffer = true, bool addAsChildren = true, bool autoEntityLink = true)
        {
            // sync...
            entityManager.CompleteAllJobs();

            entityManager.SetOrAddComponentData(entity, new Owner {Target = owner});

            if (!setRelative)
                return;

            // the name OwnerChildren can be confusing, but let's say you have a LivableDesc with a MovableDesc and ColliderDesc.
            // if you want to get the collider from the movable, OwnerChild will be able to help with that.
            NativeArray<OwnerChild> ownerChildren = default;

            var hasBuffer = entityManager.HasComponent(owner, typeof(OwnerChild));
            if (hasBuffer)
                ownerChildren = entityManager.GetBuffer<OwnerChild>(owner).ToNativeArray(Allocator.Temp);
            if (!hasBuffer && createChildBuffer)
            {
                ownerChildren = entityManager.AddBuffer<OwnerChild>(owner).ToNativeArray(Allocator.Temp);
                hasBuffer     = true;
            }

            var relativeGroup = entityManager.World.GetExistingSystem<RelativeGroup>();
            foreach (var componentSystemBase in relativeGroup.Systems)
            {
                var system = (ISyncEvent) componentSystemBase;
                for (var i = 0; setChildrenRelative && hasBuffer && i != ownerChildren.Length; i++)
                {
                    if (system.ComponentType.TypeIndex == ownerChildren[i].TypeId)
                        system.SyncRelativeToEntity(entity, ownerChildren[i].Child);
                }

                system.SyncRelativeToEntity(entity, owner);
            }

            if (hasBuffer)
                ownerChildren.Dispose();

            if (autoEntityLink)
                entityManager.SetOrAddComponentData(entity, new DestroyChainReaction(owner));

            if (hasBuffer && addAsChildren)
            {
                AddChildrenOwner(entityManager, entity, owner);
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
    public sealed class RelativeSync<T> : JobComponentSystem, Relative.ISyncEvent
        where T : struct, IEntityDescription
    {
        public ComponentType ComponentType => ComponentType.ReadWrite<T>();

        protected override void OnCreate()
        {
            World.GetOrCreateSystem<AppEventSystem>().SubscribeToAll(this);

            // TODO: Add ways to synchronize relative snapshots
            /*if (World.GetExistingSystem<ClientSimulationSystemGroup>() != null)
            {
                ComponentSystemGroup topGroup;
                
                var receiveSystem = World.GetOrCreateSystem<ReceiveRelativeSystem<T>>();
                topGroup = World.GetOrCreateSystem<ReceiveRelativeSystemGroup>();
                topGroup.AddSystemToUpdateList(receiveSystem);
            }

            if (World.GetExistingSystem<ServerSimulationSystemGroup>() != null)
            {
                var topGroup = World.GetOrCreateSystem<SynchronizeRelativeSystemGroup>();
                var system   = World.GetOrCreateSystem<SynchronizeRelativeSystem<T>>();

                topGroup.AddSystemToUpdateList(system);
            }*/
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

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public abstract class FindRelativeComponent : ComponentSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            var topGroup = World.GetOrCreateSystem<RelativeGroup>();
            foreach (var typeInfo in TypeManager.AllTypes)
            {
                var descriptionType = typeInfo.Type?.GetInterfaces().FirstOrDefault(t => t == typeof(IEntityDescription));
                if (descriptionType == null)
                    continue;

                var systemType = typeof(RelativeSync<>).MakeGenericType(typeInfo.Type);
                topGroup.AddSystemToUpdateList(World.GetOrCreateSystem(systemType));
            }
        }

        protected override void OnUpdate()
        {

        }
    }

    // Todo: find a way to synchronize it nicely.
    public struct Relative<TDescription> : IComponentData, IReadWriteComponentSnapshot<Relative<TDescription>, GhostSetup>
        where TDescription : struct, IEntityDescription
    {
        public Entity Target;

        public Relative(Entity target)
        {
            Target = target;
        }

        public void WriteTo(DataStreamWriter writer, ref Relative<TDescription> baseline, GhostSetup setup, SerializeClientData jobData)
        {
            writer.WritePackedUInt(setup[Target], jobData.NetworkCompressionModel);
        }

        public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Relative<TDescription> baseline, DeserializeClientData jobData)
        {
            var ghostId = reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
            jobData.GhostToEntityMap.TryGetValue(ghostId, out Target);
        }
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