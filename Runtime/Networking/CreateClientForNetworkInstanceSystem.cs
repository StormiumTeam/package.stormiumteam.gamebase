using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.shared.ecs;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Networking
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(UpdateLoop.IntEnd))]
    public class CreateClientForNetworkInstanceSystem : NetworkComponentSystem
    {
        private EntityArchetype m_ClientArchetype;
        private ComponentGroup  m_Group, m_DestroyClientGroup;
        private ModelIdent m_GamePlayerModel;

        protected override void OnCreateManager()
        {
            m_ClientArchetype    = EntityManager.CreateArchetype(typeof(ClientTag), typeof(NetworkClient), typeof(ClientToNetworkInstance));
            m_Group              = GetComponentGroup(typeof(NetworkInstanceData), ComponentType.Exclude<NetworkInstanceToClient>());
            m_DestroyClientGroup = GetComponentGroup(typeof(ClientTag), typeof(ClientToNetworkInstance));

            m_GamePlayerModel = World.GetOrCreateManager<GamePlayerProvider>().GetModelIdent();
        }

        public override void OnNetworkInstanceAdded(int instanceId, Entity instanceEntity)
        {
            base.OnNetworkInstanceAdded(instanceId, instanceEntity);

            var gameMgr     = World.GetExistingManager<GameManager>();
            var localClient = gameMgr.Client;

            var instanceData = EntityManager.GetComponentData<NetworkInstanceData>(instanceEntity);
            var clientEntity = instanceData.IsLocal() ? localClient : EntityManager.CreateEntity(m_ClientArchetype);

            EntityManager.SetOrAddComponentData(clientEntity, new ClientToNetworkInstance(instanceEntity));
            EntityManager.SetOrAddComponentData(instanceEntity, new NetworkInstanceToClient(clientEntity));

            if (instanceData.InstanceType == InstanceType.Client
                || instanceData.InstanceType == InstanceType.LocalServer)
            {
                Entity gamePlayer;
                if (!EntityManager.HasComponent<NetworkClientToGamePlayer>(clientEntity))
                {
                    EntityManager.AddComponent(clientEntity, typeof(NetworkClientToGamePlayer));

                    gamePlayer = gameMgr.SpawnLocal(m_GamePlayerModel);

                    if (instanceData.IsLocal())
                        EntityManager.SetComponentData(gamePlayer, new GamePlayer(0, true));

                    EntityManager.AddComponent(gamePlayer, typeof(GamePlayerToNetworkClient));
                }
                else
                {
                    // it shouldn't happen?
                    Debug.LogWarning($"Shouldn't happen (except if we created a server after another local instance (curr instancetype={instanceData.InstanceType})).");
                    gamePlayer = EntityManager.GetComponentData<NetworkClientToGamePlayer>(clientEntity).Target;
                }

                EntityManager.SetComponentData(clientEntity, new NetworkClientToGamePlayer(gamePlayer));
                EntityManager.SetComponentData(gamePlayer, new GamePlayerToNetworkClient(clientEntity));
            }
        }

        protected override void OnUpdate()
        {          
            var gameMgr     = World.GetExistingManager<GameManager>();
            var localClient = gameMgr.Client;
            
            ForEach((Entity clientEntity, ref ClientToNetworkInstance clientToNetworkInstance) =>
            {
                if (EntityManager.Exists(clientToNetworkInstance.Target) || clientEntity == localClient)
                    return;

                Debug.Log("Destroyed client.");
                PostUpdateCommands.DestroyEntity(clientEntity);
            }, m_DestroyClientGroup);
            
            ForEach((Entity entity, ref GamePlayerToNetworkClient gamePlayerToNetworkClient) =>
            {
                if (!EntityManager.Exists(gamePlayerToNetworkClient.Target))
                    PostUpdateCommands.DestroyEntity(entity);
            });
        }
    }
}