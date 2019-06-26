using DefaultNamespace;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumTeam.Networking.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace StormiumTeam.GameBase
{
    public struct GamePlayer : IComponentFromSnapshot<GamePlayerSnapshot>
    {
        public ulong MasterServerId;
        public int   ServerId;
        public bool  IsSelf;

        public GamePlayer(ulong masterServerId, bool isSelf)
        {
            MasterServerId = masterServerId;
            IsSelf         = isSelf;
            ServerId       = -1;
        }

        public class UpdateFromSnapshot : BaseUpdateFromSnapshotSystem<GamePlayerSnapshot, GamePlayer>
        {
        }

        public void Set(GamePlayerSnapshot snapshot, NativeHashMap<int, GhostEntity> ghostMap)
        {
            MasterServerId = snapshot.MasterServerId;
            ServerId       = snapshot.ServerId;
        }
    }

    public class UpdateCameraFromSnapshot : BaseUpdateFromSnapshotSystem<GamePlayerSnapshot, ServerCameraState>
    {
    }

    public struct GamePlayerSnapshot : ISnapshotData<GamePlayerSnapshot>
    {
        public uint Tick { get; set; }

        public CameraStateSnapshotFormat CameraSnapshotFormat;

        public ulong MasterServerId;
        public int   ServerId;

        public void PredictDelta(uint tick, ref GamePlayerSnapshot baseline1, ref GamePlayerSnapshot baseline2)
        {
            throw new System.NotImplementedException();
        }

        public void Serialize(ref GamePlayerSnapshot baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
        {
            writer.WritePackedIntDelta(ServerId, baseline.ServerId, compressionModel);

            var unionMasterServerId         = new ULongUIntUnion {LongValue = MasterServerId};
            var baselineUnionMasterServerId = new ULongUIntUnion {LongValue = baseline.MasterServerId};

            writer.WritePackedUIntDelta(unionMasterServerId.Int0Value, baselineUnionMasterServerId.Int0Value, compressionModel);
            writer.WritePackedUIntDelta(unionMasterServerId.Int1Value, baselineUnionMasterServerId.Int1Value, compressionModel);

            CameraSnapshotFormat.Write(writer, baseline.CameraSnapshotFormat, compressionModel);
        }

        public void Deserialize(uint tick, ref GamePlayerSnapshot baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
        {
            Tick = tick;

            ServerId = reader.ReadPackedIntDelta(ref ctx, baseline.ServerId, compressionModel);

            var baselineUnion = new ULongUIntUnion {LongValue = baseline.MasterServerId};

            var u1 = reader.ReadPackedUIntDelta(ref ctx, baselineUnion.Int0Value, compressionModel);
            var u2 = reader.ReadPackedUIntDelta(ref ctx, baselineUnion.Int1Value, compressionModel);

            MasterServerId = new ULongUIntUnion {Int0Value = u1, Int1Value = u2}.LongValue;

            CameraSnapshotFormat.Read(reader, baseline.CameraSnapshotFormat, compressionModel, ref ctx);
        }

        public void Interpolate(ref GamePlayerSnapshot target, float factor)
        {
            ServerId       = target.ServerId;
            MasterServerId = target.MasterServerId;

            CameraSnapshotFormat.Interpolate(target.CameraSnapshotFormat, factor);
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

        public GhostComponentType<GamePlayer>        GhostPlayerType;
        public GhostComponentType<ServerCameraState> GhostCameraStateType;

        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;

        public void BeginSerialize(ComponentSystemBase system)
        {
            system.GetGhostComponentType(out GhostPlayerType);
            system.GetGhostComponentType(out GhostCameraStateType);

            GhostStateFromEntity = system.GetComponentDataFromEntity<GhostSystemStateComponent>(true);
        }

        public bool CanSerialize(EntityArchetype arch)
        {
            var matches = 0;
            var types   = arch.GetComponentTypes();
            for (var i = 0; i != types.Length; i++)
            {
                if (types[i] == GhostPlayerType) matches++;
                if (types[i] == GhostCameraStateType) matches++;
            }

            return matches == 2;
        }

        public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref GamePlayerSnapshot snapshot)
        {
            var player = chunk.GetNativeArray(GhostPlayerType.Archetype)[ent];
            snapshot.Tick           = tick;
            snapshot.ServerId       = player.ServerId;
            snapshot.MasterServerId = player.MasterServerId;

            var camera = chunk.GetNativeArray(GhostCameraStateType.Archetype)[ent];
            snapshot.CameraSnapshotFormat.TargetGhostId = GhostStateFromEntity.Exists(camera.Target) ? (uint) GhostStateFromEntity[camera.Target].ghostId : 0;
            snapshot.CameraSnapshotFormat.CameraMode    = camera.Mode;
            snapshot.CameraSnapshotFormat.SetTransform(camera.Offset);
        }
    }

    public class GamePlayerGhostSpawnSystem : DefaultGhostSpawnSystem<GamePlayerSnapshot>
    {
        protected override EntityArchetype GetGhostArchetype()
        {
            return EntityManager.CreateArchetype
            (
                typeof(GamePlayer),
                typeof(LocalCameraState),
                typeof(ServerCameraState),
                typeof(GamePlayerSnapshot),
                typeof(ReplicatedEntityComponent)
            );
        }

        protected override EntityArchetype GetPredictedGhostArchetype()
        {
            return EntityManager.CreateArchetype
            (
                typeof(GamePlayer),
                typeof(LocalCameraState),
                typeof(ServerCameraState),
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
                ComponentType.ReadWrite<GamePlayer>(),
                typeof(LocalCameraState),
                typeof(ServerCameraState),
            };
        }
    }
}