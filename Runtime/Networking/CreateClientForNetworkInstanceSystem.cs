using package.stormiumteam.networking.runtime.highlevel;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Networking
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class CreateClientForNetworkInstanceSystem : BaseComponentSystem
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

        protected override void OnUpdate()
        {
            var gameMgr = World.GetExistingManager<GameManager>();
            var localClient = gameMgr.Client;
            
            using (var entityArray = m_Group.ToEntityArray(Allocator.TempJob))
            using (var dataArray = m_Group.ToComponentDataArray<NetworkInstanceData>(Allocator.TempJob))
            {
                for (var i = 0; i != entityArray.Length; i++)
                {
                    var instanceEntity = entityArray[i];
                    var instanceData   = dataArray[i];
                    
                    var clientEntity   = instanceData.IsLocal() ? localClient : EntityManager.CreateEntity(m_ClientArchetype);
                    if (!EntityManager.HasComponent<ClientToNetworkInstance>(clientEntity))
                        EntityManager.AddComponentData(clientEntity, new ClientToNetworkInstance(instanceEntity));
                    else
                        EntityManager.SetComponentData(clientEntity, new ClientToNetworkInstance(instanceEntity));
                    EntityManager.AddComponentData(instanceEntity, new NetworkInstanceToClient(clientEntity));

                    if (instanceData.InstanceType == InstanceType.Client
                        || instanceData.InstanceType == InstanceType.LocalServer)
                    {
                        Entity gamePlayer = default;
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
                            Debug.LogWarning("Shouldn't happen.");
                            gamePlayer = EntityManager.GetComponentData<NetworkClientToGamePlayer>(clientEntity).Target;
                        }

                        EntityManager.SetComponentData(clientEntity, new NetworkClientToGamePlayer(gamePlayer));
                        EntityManager.SetComponentData(gamePlayer, new GamePlayerToNetworkClient(clientEntity));
                    }
                }
            }

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