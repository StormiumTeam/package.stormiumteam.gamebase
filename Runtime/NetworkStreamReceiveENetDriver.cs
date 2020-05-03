namespace StormiumTeam.GameBase
{
#if ENET_DRIVER
    public sealed unsafe class NetworkStreamReceiveENetDriver : NetworkStreamReceiveSystem<ENetDriver>
    {
        protected override void OnCreate()
        {
            if (!Library.Initialized)
            {
                if (!Library.Initialize())
                    throw new InvalidOperationException("Library not initialized");

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
            address.SetHost(ip.Address.ToString());
            address.Port = (ushort) ip.Port;
            Debug.Log($"s={ip.Address.ToString()}:{ip.Port}");

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
            address.SetHost(ip.Address.ToString());
            address.Port = (ushort) ip.Port;
            Debug.Log($"s={ip.Address.ToString()}:{ip.Port}");
            
            var ent = EntityManager.CreateEntity();
            EntityManager.AddComponentData(ent, new NetworkStreamConnection {Value = m_Driver.Connect(address)});
            EntityManager.AddComponentData(ent, new NetworkSnapshotAckComponent());
            EntityManager.AddComponentData(ent, new CommandTargetComponent());

            EntityManager.AddBuffer<OutgoingRpcDataStreamBufferComponent>(ent).Capacity = (100);
            EntityManager.AddBuffer<IncomingCommandDataStreamBufferComponent>(ent).Capacity = (100);
            EntityManager.AddBuffer<IncomingSnapshotStreamBufferComponent>(ent).Capacity = (100);
            EntityManager.AddBuffer<IncomingRpcDataStreamBufferComponent>(ent).Capacity = (100);
            return ent;
        }

        protected override ENetDriver CreateDriver()
        {
            var driver = new ENetDriver(64);
            return driver;
        }
    }
#endif
}