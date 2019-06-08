using DefaultNamespace;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase
{
    public struct GamePlayer : IComponentFromSnapshot<GamePlayerSnapshot>
    {
        public ulong MasterServerId;
        public int ServerId;
        public bool  IsSelf;

        public GamePlayer(ulong masterServerId, bool isSelf)
        {
            MasterServerId = masterServerId;
            IsSelf        = isSelf;
            ServerId = -1;
        }
        
        public class UpdateFromSnapshot : BaseUpdateFromSnapshotSystem<GamePlayerSnapshot, GamePlayer>
        {}

        public void Set(GamePlayerSnapshot snapshot)
        {
            MasterServerId = snapshot.MasterServerId;
            ServerId = snapshot.ServerId;
        }
    }
    
    public struct GamePlayerSnapshot : ISnapshotData<GamePlayerSnapshot>
    {
        public uint Tick { get; set; }

        public ulong MasterServerId;
        public int ServerId;
        
        public void PredictDelta(uint tick, ref GamePlayerSnapshot baseline1, ref GamePlayerSnapshot baseline2)
        {
            throw new System.NotImplementedException();
        }

        public void Serialize(ref GamePlayerSnapshot baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
        {
            writer.WritePackedInt(ServerId, compressionModel);

            var (u1, u2) = StMath.ULongToDoubleUInt(MasterServerId);
            writer.WritePackedUInt(u1, compressionModel);
            writer.WritePackedUInt(u2, compressionModel);
        }

        public void Deserialize(uint tick, ref GamePlayerSnapshot baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
        {
            Tick = tick;
            
            ServerId = reader.ReadPackedInt(ref ctx, compressionModel);

            var u1 = reader.ReadPackedUInt(ref ctx, compressionModel);
            var u2 = reader.ReadPackedUInt(ref ctx, compressionModel);

            MasterServerId = StMath.DoubleUIntToULong(u1, u2);
        }

        public void Interpolate(ref GamePlayerSnapshot target, float factor)
        {
            ServerId = target.ServerId;
            MasterServerId = target.MasterServerId;
        }
    }

    public struct GamePlayerGhostSerializer : IGhostSerializer<GamePlayerSnapshot>
    {
        public int SnapshotSize => UnsafeUtility.SizeOf<GamePlayerSnapshot>();

        public int CalculateImportance(ArchetypeChunk chunk)
        {
            return 1; // the creation of the entity only matter
        }

        public bool WantsPredictionDelta => false;

        public ComponentType                           ComponentTypePlayer;
        public ArchetypeChunkComponentType<GamePlayer> GhostPlayerType;

        public void BeginSerialize(ComponentSystemBase system)
        {
            ComponentTypePlayer = ComponentType.ReadWrite<GamePlayer>();
            GhostPlayerType     = system.GetArchetypeChunkComponentType<GamePlayer>();
        }

        public bool CanSerialize(EntityArchetype arch)
        {
            var types = arch.GetComponentTypes();
            for (var i = 0; i != types.Length; i++)
            {
                if (types[i] == ComponentTypePlayer)
                    return true;
            }

            return false;
        }

        public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref GamePlayerSnapshot snapshot)
        {
            var player = chunk.GetNativeArray(GhostPlayerType)[ent];
            snapshot.Tick           = tick;
            snapshot.ServerId       = player.ServerId;
            snapshot.MasterServerId = player.MasterServerId;
        }
    }
    
    public class GamePlayerGhostSpawnSystem : DefaultGhostSpawnSystem<GamePlayerSnapshot>
    {
        protected override EntityArchetype GetGhostArchetype()
        {
            return EntityManager.CreateArchetype
            (
                typeof(GamePlayer),
                typeof(GamePlayerSnapshot),
                typeof(ReplicatedEntityComponent)
            );
        }

        protected override EntityArchetype GetPredictedGhostArchetype()
        {
            return EntityManager.CreateArchetype
            (
                typeof(GamePlayer),
                typeof(GamePlayerSnapshot),
                typeof(ReplicatedEntityComponent),
                typeof(PredictedEntityComponent)
            );
        }
    }

    public class GamePlayerProvider : BaseProvider
    {
        public override void GetComponents(out ComponentType[] entityComponents)
        {
            entityComponents = new[]
            {
                ComponentType.ReadWrite<GamePlayer>()
            };
        }
    }
}