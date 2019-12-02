using System;
using System.Net;
using System.Text;
using Unity.NetCode;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;
using Valve.Sockets;

namespace StormiumTeam.GameBase
{
#if VALVE_DRIVER
    public sealed unsafe class NetworkStreamReceiveValveDriver : NetworkStreamReceiveSystem<ValveDriver>
    {
        protected override void OnCreate()
        {
            if (!Library.Initialized)
            {
                StringBuilder strBuilder = new StringBuilder(1024);
                Library.Initialize(strBuilder);
                Debug.LogError($"{strBuilder.ToString()}");

                Application.quitting += Library.Deinitialize;
            }

            base.OnCreate();
        }

        public override bool Listen(IPEndPoint ip)
        {
            if (m_UnreliablePipeline == NetworkPipeline.Null)
                m_UnreliablePipeline = m_Driver.CreatePipeline(typeof(NullPipelineStage));
            if (m_RpcPipeline == NetworkPipeline.Null)
                m_RpcPipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            if (m_SnapshotPipeline == NetworkPipeline.Null)
                m_SnapshotPipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

            // Switching to server mode
            var address = new Address();
            address.SetAddress(ip.Address.ToString(), (ushort) ip.Port);
            Debug.Log($"s={ip.Address.ToString()}:{ip.Port}");
            for (var i = 0; i != 16; i++)
                Debug.Log($"s={address.ip[i]}");

            if (m_Driver.Bind(address) != 0)
                return false;
            if (m_Driver.Listen() != 0)
                return false;
            m_DriverListening = true;
            return true;
        }

        public override Entity Connect(IPEndPoint ip)
        {
            if (m_UnreliablePipeline == NetworkPipeline.Null)
                m_UnreliablePipeline = m_Driver.CreatePipeline(typeof(NullPipelineStage));
            if (m_RpcPipeline == NetworkPipeline.Null)
                m_RpcPipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            if (m_SnapshotPipeline == NetworkPipeline.Null)
                m_SnapshotPipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

            var address = new Address();
            address.SetAddress(ip.Address.ToString(), (ushort) ip.Port);
            Debug.Log($"c={ip.Address.ToString()}:{ip.Port}");
            for (var i = 0; i != 16; i++)
                Debug.Log($"c={address.ip[i]}");
            
            var ent = EntityManager.CreateEntity();
            EntityManager.AddComponentData(ent, new NetworkStreamConnection {Value = m_Driver.Connect(address)});
            EntityManager.AddComponentData(ent, new NetworkSnapshotAckComponent());
            EntityManager.AddComponentData(ent, new CommandTargetComponent());

            EntityManager.AddBuffer<OutgoingRpcDataStreamBufferComponent>(ent).Reserve(100);
            EntityManager.AddBuffer<IncomingCommandDataStreamBufferComponent>(ent).Reserve(100);
            EntityManager.AddBuffer<IncomingSnapshotStreamBufferComponent>(ent).Reserve(100);
            EntityManager.AddBuffer<IncomingRpcDataStreamBufferComponent>(ent).Reserve(100);
            return ent;
        }

        protected override ValveDriver CreateDriver()
        {
            var driver = new ValveDriver(0);
            var utils = new NetworkingUtils();
            utils.SetDebugCallback(DebugType.Verbose, (type, message) =>
            {
                Debug.Log($"[{type}] {message}");
            });

            return driver;
        }
    }
#endif
}