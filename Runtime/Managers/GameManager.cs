using System;
using System.Net;
using package.stormiumteam.networking.runtime.highlevel;
using Runtime.Data;
using StormiumTeam.GameBase;
using StormiumShared.Core.Networking;
using Unity.Entities;
using UnityEngine;

namespace Runtime
{
    [Flags]
    public enum GameType
    {
        Client = 1,
        Server = 2,
        Global = 3
    }

    public class GameManager : BaseComponentSystem
    {
        public  GameType GameType => m_GameType;
        private GameType m_GameType;

        public  Entity Client => m_Client;
        private Entity m_Client;

        public  EntityModelManager EntityModelManager => m_EntityModelManager;
        private EntityModelManager m_EntityModelManager;

        public  StormiumGameServerManager ServerManager => m_ServerManager;
        private StormiumGameServerManager m_ServerManager;

        protected override void OnCreateManager()
        {
            m_Client = EntityManager.CreateEntity
            (
                typeof(NetworkClient),
                typeof(NetworkLocalTag)
            );

            m_EntityModelManager = World.GetExistingManager<EntityModelManager>();
            m_ServerManager      = World.GetExistingManager<StormiumGameServerManager>();
        }

        private Entity client;

        protected override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (client != default)
                    EntityManager.DestroyEntity(client);
                
                var model = World.GetExistingManager<StGamePlayerProvider>().GetModelIdent();
                client = EntityManager.CreateEntity(typeof(ClientTag), typeof(NetworkClient));
                
                EntityManager.AddComponent(client, typeof(StNetworkClientToGamePlayer));

                var player = SpawnLocal(model);

                EntityManager.AddComponent(player, typeof(StGamePlayerToNetworkClient));
            }
        }

        public Entity SpawnLocal(ModelIdent ident, bool assignAuthority = true)
        {
            var fakeLocalRuntime = default(StSnapshotRuntime);

            fakeLocalRuntime.Header.Sender = new SnapshotSender(m_Client, SnapshotFlags.Local);

            var entity = EntityModelManager.SpawnEntity(ident.Id, default, fakeLocalRuntime);
            if (assignAuthority && !EntityManager.HasComponent<EntityAuthority>(entity))
            {
                EntityManager.AddComponent(entity, typeof(EntityAuthority));
            }

            return entity;
        }

        public void Unspawn(Entity entity)
        {

        }

        public void SetGameAs(GameType gameType)
        {
            m_GameType = gameType;
        }
    }

    public class StormiumGameServerManager : BaseComponentSystem
    {
        private NetworkManager m_NetworkManager;

        /// <summary>
        /// This is the server entity, it's null for a local server.
        /// </summary>
        public Entity ConnectedServerEntity;

        /// <summary>
        /// This is the server instance id, it's 0 for a local server.
        /// </summary>
        public int ConnectedServerId;

        /// <summary>
        /// This is the host entity, it can be a local client or a local server.
        /// </summary>
        public Entity HostEntity;

        /// <summary>
        /// This is the host instance id.
        /// </summary>
        public int HostId;

        protected override void OnCreateManager()
        {
            m_NetworkManager = World.GetExistingManager<NetworkManager>();
        }

        protected override void OnUpdate()
        {

        }

        public bool ConnectToServer(IPEndPoint endPoint)
        {
            var r = m_NetworkManager.StartClient(endPoint);

            ConnectedServerEntity = r.ServerInstanceEntity;
            ConnectedServerId     = r.ServerInstanceId;
            HostEntity            = r.ClientInstanceEntity;
            HostId                = r.ClientInstanceId;

            return !r.IsError;
        }

        public bool LaunchServer(int port)
        {
            var r = m_NetworkManager.StartServer(new IPEndPoint(IPAddress.Any, port));

            ConnectedServerEntity = default;
            ConnectedServerId     = 0;
            HostEntity            = r.Entity;
            HostId                = r.InstanceId;

            return !r.IsError;
        }

        public void StopEverything()
        {
            m_NetworkManager.StopAll();
        }
    }
}