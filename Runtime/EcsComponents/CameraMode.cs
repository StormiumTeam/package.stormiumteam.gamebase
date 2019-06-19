using Unity.Entities;
using Unity.Mathematics;
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

    public struct CameraState : IComponentData
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

    public struct ServerCameraState : IComponentData
    {
        public CameraState Data;

        public CameraMode     Mode   => Data.Mode;
        public Entity         Target => Data.Target;
        public RigidTransform Offset => Data.Offset;
    }

    public struct CameraStateSnapshotFormat
    {
        public const int Quantization    = 1000;
        public const int InvQuantization = 1 / 1000;

        public uint       TargetGhostId;
        public CameraMode CameraMode;

        public int3 Position;
        public int4 Rotation;

        public void Write(DataStreamWriter writer, NetworkCompressionModel compressionModel)
        {
            var useOffset = Position.x != 0 && Position.y != 0 && Position.z != 0
                            && Rotation.x != 0 && Rotation.y != 0 && Rotation.z != 0 && Rotation.w != 0;

            writer.WritePackedUInt(TargetGhostId, compressionModel);
            writer.WritePackedUInt((uint) CameraMode, compressionModel);
            writer.WritePackedUInt(useOffset ? (uint) 1 : 0, compressionModel);
            if (!useOffset)
                return;

            writer.WritePackedInt(Position.x, compressionModel);
            writer.WritePackedInt(Position.y, compressionModel);
            writer.WritePackedInt(Position.z, compressionModel);

            writer.WritePackedInt(Rotation.x, compressionModel);
            writer.WritePackedInt(Rotation.y, compressionModel);
            writer.WritePackedInt(Rotation.z, compressionModel);
            writer.WritePackedInt(Rotation.w, compressionModel);
        }

        public void Read(DataStreamReader reader, NetworkCompressionModel compressionModel, ref DataStreamReader.Context ctx)
        {
            TargetGhostId = reader.ReadPackedUInt(ref ctx, compressionModel);
            CameraMode    = (CameraMode) reader.ReadPackedUInt(ref ctx, compressionModel);
            var useOffset = reader.ReadPackedUInt(ref ctx, compressionModel) == 1;
            if (!useOffset)
                return;

            Position.x = reader.ReadPackedInt(ref ctx, compressionModel);
            Position.y = reader.ReadPackedInt(ref ctx, compressionModel);
            Position.z = reader.ReadPackedInt(ref ctx, compressionModel);

            Rotation.x = reader.ReadPackedInt(ref ctx, compressionModel);
            Rotation.y = reader.ReadPackedInt(ref ctx, compressionModel);
            Rotation.z = reader.ReadPackedInt(ref ctx, compressionModel);
            Rotation.w = reader.ReadPackedInt(ref ctx, compressionModel);
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