using System;
using System.Collections.Generic;
using System.Linq;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase
{
    public interface ISystemProviderExcludeComponents : IAppEvent
    {
        void ExcludeComponentsFor(Type type, List<ComponentType> components);
    }

    public class SystemProviderGroup : ComponentSystemGroup
    {
    }

    public abstract class SystemProvider : SystemProvider<SystemProvider.NoData>
    {
        public struct NoData
        {}

        public override Entity SpawnLocalEntityWithArguments(NoData data)
        {
            throw new NotImplementedException();
        }
    }
    
    [UpdateInGroup(typeof(SystemProviderGroup))]
    public abstract class SystemProvider<TCreateData> : ComponentSystem, ISnapshotManageForClient, ISystemProviderExcludeComponents
    where TCreateData : struct
    {
        private EntityModelManager m_ModelManager;
        private GameManager m_GameManager;
        private ModelIdent m_ModelIdent;
        private PatternResult m_SnapshotPattern;
        
        private ComponentType[] m_EntityComponents;
        private ComponentType[] m_ExcludedComponents;

        private BlockComponentSerialization[] m_BlockedComponents;

        public ComponentType[] EntityComponents => m_EntityComponents;
        public ComponentType[] ExcludedComponents => m_ExcludedComponents;
        public EntityArchetype EntityArchetype { get; protected set; }

        public ComponentType[] ComponentsToExcludeFromStreamers { get; private set; }

        protected NativeList<TCreateData> CreateEntityDelayed;

        private bool m_CanHaveDelayedEntities;
        
        protected override void OnCreateManager()
        {
            if ((m_CanHaveDelayedEntities = typeof(TCreateData) != typeof(SystemProvider.NoData)) == true)
            {
                CreateEntityDelayed = new NativeList<TCreateData>(32, Allocator.Persistent);
            }

            GetManager();
        }

        protected override void OnUpdate()
        {
            if (m_CanHaveDelayedEntities) 
                FlushDelayedEntities();
        }

        public NativeList<TCreateData> GetEntityDelayedList()
        {
            if (!m_CanHaveDelayedEntities)
                throw new NotImplementedException();
            
            return CreateEntityDelayed;
        }

        public void FlushDelayedEntities()
        {
            for (var i = 0; i != CreateEntityDelayed.Length; i++)
            {
                SpawnLocalEntityWithArguments(CreateEntityDelayed[i]);
            }
            
            CreateEntityDelayed.Clear();
        }

        public EntityModelManager GetManager()
        {
            if (m_ModelManager == null)
            {
                m_ModelManager = World.GetExistingManager<EntityModelManager>();
                m_GameManager  = World.GetExistingManager<GameManager>();

                GetComponents(out m_EntityComponents, out m_ExcludedComponents);
                if (EntityComponents == null && ExcludedComponents == null)
                {
                    m_ModelIdent = m_ModelManager.Register
                    (
                        $"EntityProvider.{GetType().Name}", SpawnEntity, DestroyEntity
                    );
                }
                else
                {
                    // todo: I was lazy when making this, this should be remade as it's slow
                    var l = new Dictionary<ComponentType, byte> {[ComponentType.ReadWrite<ModelIdent>()] = 0};
                    
                    // Remove duplicates and add ModelIdent component
                    if (m_EntityComponents != null)
                        foreach (var c in m_EntityComponents)
                            l[c] = 0;
                    m_EntityComponents = l.Keys.ToArray();
                    l.Clear();
                    
                    if (m_ExcludedComponents != null)
                        foreach (var c in m_ExcludedComponents)
                            l[c] = 0;
                    var foreignList = new List<ComponentType>();
                    foreach (var obj in AppEvent<ISystemProviderExcludeComponents>.GetObjEvents())
                        if (obj != this)
                            obj.ExcludeComponentsFor(GetType(), foreignList);
                    foreach (var c in foreignList)
                        l[c] = 0;

                    ComponentsToExcludeFromStreamers = l.Keys.ToArray();
                    m_BlockedComponents              = new BlockComponentSerialization[ComponentsToExcludeFromStreamers.Length];

                    for (var i = 0; i != ComponentsToExcludeFromStreamers.Length; i++)
                    {
                        m_BlockedComponents[i] = new BlockComponentSerialization {TypeIdx = ComponentsToExcludeFromStreamers[i].TypeIndex};
                    }

                    EntityArchetype = EntityManager.CreateArchetype(EntityComponents);

                    var patternName = $"EntityProvider.Full.{GetType().Name}";
                    m_ModelIdent = m_ModelManager.RegisterFull
                    (
                        patternName + ".Model", ComponentsToExcludeFromStreamers, ProviderSpawnEntity, ProviderDestroyEntity, SerializeCollection, DeserializeCollection
                    );

                    m_SnapshotPattern = World.GetOrCreateManager<NetPatternSystem>().GetLocalBank().Register(new PatternIdent(patternName + ".Snapshot"));
                    World.GetOrCreateManager<AppEventSystem>().SubscribeToAll(this);
                }
            }

            return m_ModelManager;
        }

        public ModelIdent GetModelIdent()
        {
            return m_ModelIdent;
        }

        protected virtual Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
        {
            return EntityManager.CreateEntity(EntityArchetype);
        }

        protected virtual void DestroyEntity(Entity worldEntity)
        {
            EntityManager.DestroyEntity(worldEntity);
        }

        public Entity ProviderSpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
        {
            var e = SpawnEntity(origin, snapshotRuntime);

            if (ComponentsToExcludeFromStreamers != null)
            {
                var blockedComponents = EntityManager.AddBuffer<BlockComponentSerialization>(e);
                blockedComponents.CopyFrom(m_BlockedComponents);
            }

            return e;
        }

        public void ProviderDestroyEntity(Entity worldEntity)
        {
            DestroyEntity(worldEntity);
        }
        
        public virtual void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedStreamerComponents)
        {
            entityComponents = null;
            excludedStreamerComponents = null;
        }
        
        public virtual void SerializeCollection(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime snapshotRuntime)
        {}
        
        public virtual void DeserializeCollection(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime snapshotRuntime)
        {}

        public abstract Entity SpawnLocalEntityWithArguments(TCreateData data);
        
        public virtual Entity SpawnLocalEntityDelayed(EntityCommandBuffer entityCommandBuffer)
        {
            throw new NotImplementedException();
        }
        
        public Entity SpawnLocal()
        {
            return m_GameManager.SpawnLocal(GetModelIdent());
        }

        // Snapshot Implementation
        public PatternResult GetSystemPattern()
        {
            return m_SnapshotPattern;
        }

        public DataBufferWriter WriteData(SnapshotReceiver receiver, SnapshotRuntime runtime)
        {
            var buffer = new DataBufferWriter(4096, Allocator.TempJob);
            var lengthMarker = buffer.WriteInt(0);
            SerializeCollection(ref buffer, receiver, runtime);
            buffer.WriteInt(buffer.Length, lengthMarker);
            return buffer;
        }

        public void ReadData(SnapshotSender sender, SnapshotRuntime runtime, DataBufferReader sysData)
        {
            var length = sysData.ReadValue<int>();
            DeserializeCollection(ref sysData, sender, runtime);
            if (length != sysData.CurrReadIndex)
            {
                Debug.LogError($"{GetType()}.ReadData() -> Error! -> length({length}) != sysData.CurrReadIndex({sysData.CurrReadIndex})");
            }
        }

        public virtual void ExcludeComponentsFor(Type type, List<ComponentType> components)
        {
            
        }
    }
}