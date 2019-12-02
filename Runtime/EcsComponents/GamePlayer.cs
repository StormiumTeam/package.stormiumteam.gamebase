using package.stormiumteam.shared;
using Revolution;
using Unity.NetCode;
using Unity.Entities;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase
{
    public struct GamePlayer : IComponentData
    {
        public ulong MasterServerId;
        public int   ServerId;
        
        public GamePlayer(ulong masterServerId)
        {
            MasterServerId = masterServerId;
            ServerId       = -1;
        }
    }

    public struct GamePlayerSnapshot : IReadWriteSnapshot<GamePlayerSnapshot>, ISynchronizeImpl<GamePlayer>, ISnapshotDelta<GamePlayerSnapshot>
    {
        public uint Tick { get; set; }

        public ulong MasterServerId;
        public int   ServerId;

        public void WriteTo(DataStreamWriter writer, ref GamePlayerSnapshot baseline, NetworkCompressionModel compressionModel)
        {
            writer.WritePackedIntDelta(ServerId, baseline.ServerId, compressionModel);

            var unionMasterServerId         = new ULongUIntUnion {LongValue = MasterServerId};
            var baselineUnionMasterServerId = new ULongUIntUnion {LongValue = baseline.MasterServerId};

            writer.WritePackedUIntDelta(unionMasterServerId.Int0Value, baselineUnionMasterServerId.Int0Value, compressionModel);
            writer.WritePackedUIntDelta(unionMasterServerId.Int1Value, baselineUnionMasterServerId.Int1Value, compressionModel);
        }

        public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref GamePlayerSnapshot baseline, NetworkCompressionModel compressionModel)
        {
            ServerId = reader.ReadPackedIntDelta(ref ctx, baseline.ServerId, compressionModel);

            var baselineUnion = new ULongUIntUnion {LongValue = baseline.MasterServerId};

            var u1 = reader.ReadPackedUIntDelta(ref ctx, baselineUnion.Int0Value, compressionModel);
            var u2 = reader.ReadPackedUIntDelta(ref ctx, baselineUnion.Int1Value, compressionModel);

            MasterServerId = new ULongUIntUnion {Int0Value = u1, Int1Value = u2}.LongValue;
        }

        public void SynchronizeFrom(in GamePlayer component, in DefaultSetup setup, in SerializeClientData jobData)
        {
            ServerId       = component.ServerId;
            MasterServerId = component.MasterServerId;
        }

        public void SynchronizeTo(ref GamePlayer component, in DeserializeClientData jobData)
        {
            component.ServerId       = ServerId;
            component.MasterServerId = MasterServerId;
        }

        public bool DidChange(GamePlayerSnapshot baseline)
        {
            return !(baseline.MasterServerId == MasterServerId && baseline.ServerId == ServerId);
        }
        
        public class System : ComponentSnapshotSystemDelta<GamePlayer, GamePlayerSnapshot>
        {
            public struct Exclude : IComponentData
            {}
            
            public override ComponentType ExcludeComponent => typeof(Exclude);
        }
        
        public class Synchronize : ComponentUpdateSystemDirect<GamePlayer, GamePlayerSnapshot>
        {}
    }

    [UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities))]
    public class GamePlayerProvider : BaseProvider
    {
        public override void GetComponents(out ComponentType[] entityComponents)
        {
            entityComponents = new[]
            {
                typeof(PlayerDescription),
                ComponentType.ReadWrite<GamePlayer>(),
                typeof(LocalCameraState),
                typeof(ServerCameraState),
            };
        }
    }
}