using DefaultNamespace;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase
{
    public struct AimLookState : IComponentFromSnapshot<AimLookState.SnapshotData>
    {
        public float2 Aim;

        public struct SnapshotData : ISnapshotFromComponent<SnapshotData, AimLookState>
        {
            public uint Tick { get; private set; }

            public int2 Aim; // float * 1000

            public void PredictDelta(uint tick, ref SnapshotData baseline1, ref SnapshotData baseline2)
            {
            }

            public void Serialize(ref SnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
            {
                writer.WritePackedInt(Aim.x, compressionModel);
                writer.WritePackedInt(Aim.y, compressionModel);
            }

            public void Deserialize(uint tick, ref SnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
            {
                Tick = tick;
                
                Aim.x = reader.ReadPackedInt(ref ctx, compressionModel);
                Aim.y = reader.ReadPackedInt(ref ctx, compressionModel);
            }

            public void Interpolate(ref SnapshotData target, float factor)
            {
                Aim = (int2) math.lerp(Aim, target.Aim, factor);
            }

            public void Set(AimLookState component)
            {
                Aim = (int2) (component.Aim * 1000);
            }
        }

        public void Set(SnapshotData snapshot)
        {
            Aim = ((float2) snapshot.Aim) * 0.001f;
        }
    }
}