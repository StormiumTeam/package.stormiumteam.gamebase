using Revolution;
using Unity.NetCode;
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

    public struct ServerCameraState : IComponentData
    {
        public struct Exclude : IComponentData
        {
        }

        public CameraState Data;

        public CameraMode     Mode   => Data.Mode;
        public Entity         Target => Data.Target;
        public RigidTransform Offset => Data.Offset;

        public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISnapshotDelta<Snapshot>,
                                 ISynchronizeImpl<ServerCameraState, GhostSetup>
        {
            public uint Tick { get; set; }

            public CameraStateSnapshotFormat SnapshotFormat;

            public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
            {
                SnapshotFormat.Write(writer, baseline.SnapshotFormat, compressionModel);
            }

            public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
            {
                SnapshotFormat.Read(reader, baseline.SnapshotFormat, compressionModel, ref ctx);
            }

            public void SynchronizeFrom(in ServerCameraState component, in GhostSetup setup, in SerializeClientData jobData)
            {
                SnapshotFormat.TargetGhostId = setup[component.Target];
                SnapshotFormat.CameraMode    = component.Mode;
                SnapshotFormat.SetTransform(component.Offset);
            }

            public void SynchronizeTo(ref ServerCameraState component, in DeserializeClientData jobData)
            {
                jobData.GhostToEntityMap.TryGetValue(SnapshotFormat.TargetGhostId, out component.Data.Target);
                component.Data.Mode   = SnapshotFormat.CameraMode;
                component.Data.Offset = SnapshotFormat.GetTransform();
            }

            public bool DidChange(Snapshot baseline)
            {
                return !(SnapshotFormat.CameraMode == baseline.SnapshotFormat.CameraMode
                         && SnapshotFormat.TargetGhostId == baseline.SnapshotFormat.TargetGhostId
                         && math.all(SnapshotFormat.Position == baseline.SnapshotFormat.Position)
                         && math.all(SnapshotFormat.Rotation == baseline.SnapshotFormat.Rotation));
            }
        }

        public class System : ComponentSnapshotSystemDelta<ServerCameraState, Snapshot, GhostSetup>
        {
            public override ComponentType ExcludeComponent => typeof(Exclude);
        }

        public class Synchronize : ComponentUpdateSystemDirect<ServerCameraState, Snapshot, GhostSetup>
        {
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