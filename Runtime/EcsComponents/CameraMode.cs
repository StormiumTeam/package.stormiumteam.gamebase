using DefaultNamespace;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase
{
    public enum CameraMode
    {
        /// <summary>
        /// The camera will not be ruled by this state and will revert to Default mode if there are
        /// no other states with '<see cref="Forced"/>' mode.
        /// </summary>
        Default = 0,

        /// <summary>
        /// The camera will be forced to the rules of this state and override previous states.
        /// </summary>
        Forced = 1
    }

    public struct CameraState
    {
        public CameraMode Mode;

        public Entity         Target;
        public RigidTransform Offset;
    }

    public struct LocalCameraState : IComponentData
    {
        public CameraState Data;

        public CameraMode     Mode   => Data.Mode;
        public Entity         Target => Data.Target;
        public RigidTransform Offset => Data.Offset;
    }

    public struct ServerCameraState : IComponentData, IComponentFromSnapshot<GamePlayerSnapshot>
    {
        public CameraState Data;

        public CameraMode     Mode   => Data.Mode;
        public Entity         Target => Data.Target;
        public RigidTransform Offset => Data.Offset;

        public void Set(GamePlayerSnapshot snapshot, NativeHashMap<int, GhostEntity> ghostMap)
        {
            if (ghostMap.TryGetValue((int) snapshot.CameraSnapshotFormat.TargetGhostId, out var target) && target.valid)
                Data.Target = target.entity;
            else
                Data.Target = default;

            Data.Mode = snapshot.CameraSnapshotFormat.CameraMode;
        }
    }

    public struct CameraStateSnapshotFormat
    {
        public const int Quantization    = 1000;
        public const int InvQuantization = 1 / 1000;

        public uint       TargetGhostId;
        public CameraMode CameraMode;

        public int3 Position;
        public int4 Rotation;

        public void Write(DataStreamWriter writer, CameraStateSnapshotFormat baseline, NetworkCompressionModel compressionModel)
        {
            var useOffset = Position.x != 0 && Position.y != 0 && Position.z != 0
                            && Rotation.x != 0 && Rotation.y != 0 && Rotation.z != 0 && Rotation.w != 0;

            writer.WritePackedUIntDelta(TargetGhostId, baseline.TargetGhostId, compressionModel);
            writer.WritePackedUIntDelta((uint) CameraMode, (uint) baseline.CameraMode, compressionModel);
            writer.WritePackedUInt(useOffset ? (uint) 1 : 0, compressionModel);
            if (!useOffset)
                return;

            writer.WritePackedIntDelta(Position.x, baseline.Position.x, compressionModel);
            writer.WritePackedIntDelta(Position.y, baseline.Position.y, compressionModel);
            writer.WritePackedIntDelta(Position.z, baseline.Position.z, compressionModel);

            writer.WritePackedIntDelta(Rotation.x, baseline.Rotation.x, compressionModel);
            writer.WritePackedIntDelta(Rotation.y, baseline.Rotation.y, compressionModel);
            writer.WritePackedIntDelta(Rotation.z, baseline.Rotation.z, compressionModel);
            writer.WritePackedIntDelta(Rotation.w, baseline.Rotation.w, compressionModel);
        }

        public void Read(DataStreamReader reader, CameraStateSnapshotFormat baseline, NetworkCompressionModel compressionModel, ref DataStreamReader.Context ctx)
        {
            TargetGhostId = reader.ReadPackedUIntDelta(ref ctx, baseline.TargetGhostId, compressionModel);
            CameraMode    = (CameraMode) reader.ReadPackedUIntDelta(ref ctx, (uint) baseline.CameraMode, compressionModel);
            var useOffset = reader.ReadPackedUInt(ref ctx, compressionModel) == 1;
            if (!useOffset)
                return;

            Position.x = reader.ReadPackedIntDelta(ref ctx, baseline.Position.x, compressionModel);
            Position.y = reader.ReadPackedIntDelta(ref ctx, baseline.Position.y, compressionModel);
            Position.z = reader.ReadPackedIntDelta(ref ctx, baseline.Position.z, compressionModel);

            Rotation.x = reader.ReadPackedIntDelta(ref ctx, baseline.Rotation.x, compressionModel);
            Rotation.y = reader.ReadPackedIntDelta(ref ctx, baseline.Rotation.y, compressionModel);
            Rotation.z = reader.ReadPackedIntDelta(ref ctx, baseline.Rotation.z, compressionModel);
            Rotation.w = reader.ReadPackedIntDelta(ref ctx, baseline.Rotation.w, compressionModel);
        }

        public void SetTransform(RigidTransform rigidTransform)
        {
            Position = new int3(rigidTransform.pos * Quantization);
            Rotation = new int4(rigidTransform.rot.value * Quantization);
        }

        public RigidTransform GetTransform()
        {
            return new RigidTransform
            (
                math.normalizesafe(new quaternion(Rotation * InvQuantization)),
                Position * InvQuantization
            );
        }

        public void Interpolate(CameraStateSnapshotFormat other, float factor)
        {
            TargetGhostId = other.TargetGhostId;
            CameraMode    = other.CameraMode;

            Position = (int3) math.lerp(Position, other.Position, factor);
            Rotation = (int4) math.slerp(new quaternion(Rotation), new quaternion(other.Rotation), factor).value;
        }
    }
}