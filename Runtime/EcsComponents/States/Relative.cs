using System.Linq;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using Revolution;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;

[assembly: RegisterGenericComponentType(typeof(Relative<HitShapeDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<MovableDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<LivableDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<CharacterDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<PlayerDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<ActionDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<ActionHolderDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<ProjectileDescription>))]

namespace StormiumTeam.GameBase
{
    public interface IEntityDescription : IComponentData
    {
    }
    
    public struct ExcludeRelativeFromSnapshot : IComponentData
    {}

    public abstract class RelativeSynchronize<TDesc> : MixedComponentSnapshotSystemDelta<Relative<TDesc>, GhostSetup>
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

    public struct ActionHolderDescription : IEntityDescription
    {
        public class Sync : RelativeSynchronize<ActionHolderDescription>
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

        public Entity Target;

        public void WriteTo(DataStreamWriter writer, ref Owner baseline, GhostSetup setup, SerializeClientData jobData)
        {
            writer.WritePackedUInt(setup[Target], jobData.NetworkCompressionModel);
        }

        public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Owner baseline, DeserializeClientData jobData)
        {
            var ghostId = reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
            jobData.GhostToEntityMap.TryGetValue(ghostId, out Target);
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
            void SyncRelativeToEntity(Entity origin, Entity owner);
            void AddChildren(Entity          origin, Entity owner);

            ComponentType ComponentType { get; }
        }

        public static void AddChildrenOwner(this EntityManager entityManager, Entity source, Entity owner)
        {
            var relativeGroup = entityManager.World.GetExistingSystem<RelativeGroup>();
            foreach (var componentSystemBase in relativeGroup.Systems)
            {
                var system = (ISyncEvent) componentSystemBase;
                system.AddChildren(source, owner);
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
            {
                // If the entity had an owner before, delete it from the old linked group
                if (entityManager.HasComponent(entity, typeof(Owner)))
                {
                    var previousOwner = entityManager.GetComponentData<Owner>(entity);
                    if (entityManager.HasComponent(previousOwner.Target, typeof(LinkedEntityGroup)))
                    {
                        var previousLinkedEntityGroup = entityManager.GetBuffer<LinkedEntityGroup>(previousOwner.Target);
                        for (var i = 0; i != previousLinkedEntityGroup.Length; i++)
                        {
                            if (previousLinkedEntityGroup[i].Value == entity)
                            {
                                previousLinkedEntityGroup.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }

                if (!entityManager.HasComponent(owner, typeof(LinkedEntityGroup)))
                    entityManager.AddBuffer<LinkedEntityGroup>(owner);

                var linkedEntityGroup = entityManager.GetBuffer<LinkedEntityGroup>(owner);
                linkedEntityGroup.Add(entity);
            }

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
        }

        protected override JobHandle OnUpdate(JobHandle _)
        {
            return _;
        }

        public void AddChildren(Entity origin, Entity owner)
        {
            if (!EntityManager.HasComponent(origin, typeof(T))) 
                return;

            var buffer = EntityManager.GetBuffer<OwnerChild>(owner);
            for (var i = 0; i != buffer.Length; i++)
            {
                if (buffer[i].Child == origin)
                    return;
            }

            buffer.Add(new OwnerChild(origin, ComponentType.ReadWrite<T>()));
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
                
              //  Debug.LogError("Had description " + typeof(T));
            }

           // Debug.LogError($"({typeof(T)}) Owner {owner} added to {origin}");
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class FindRelativeComponent : ComponentSystem
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
    public struct Relative<TDescription> : IComponentData, IReadWriteComponentSnapshot<Relative<TDescription>, GhostSetup>, ISnapshotDelta<Relative<TDescription>> where TDescription : struct, IEntityDescription
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

        public bool DidChange(Relative<TDescription> baseline)
        {
            return !Target.Equals(baseline.Target);
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