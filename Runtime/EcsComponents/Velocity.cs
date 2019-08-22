using DefaultNamespace;
using Unity.Collections;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase
{
    public struct Velocity : IComponentFromSnapshot<Velocity.SnapshotData>
    {
        public struct SnapshotData : ISnapshotFromComponent<SnapshotData, Velocity>
        {
            public uint Tick { get; private set; }

            public int3 Velocity; // float * 1000

            public void PredictDelta(uint tick, ref SnapshotData baseline1, ref SnapshotData baseline2)
            {
            }

            public void Serialize(ref SnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
            {
                for (var i = 0; i != 2; i++)
                    writer.WritePackedIntDelta(Velocity[i], baseline.Velocity[i], compressionModel);
            }

            public void Deserialize(uint tick, ref SnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
            {
                Tick = tick;
                for (var i = 0; i != 2; i++)
                    Velocity[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Velocity[i], compressionModel);
            }

            public void Interpolate(ref SnapshotData target, float factor)
            {
                Velocity = new int3(math.lerp(Velocity, target.Velocity, factor));
            }

            public void Set(Velocity component)
            {
                Velocity = new int3(component.Value * 1000);
            }
        }

        public float3 Value;

        public float3 normalized => math.normalizesafe(Value);
        public float  speed      => math.length(Value);
        public float  speedSqr   => math.lengthsq(Value);

        public Velocity(float3 value)
        {
            Value = value;
        }

        public void Set(SnapshotData snapshot, NativeHashMap<int, GhostEntity> ghostMap)
        {
            Value = new float3(snapshot.Velocity) * 0.001f;
        }

        public class UpdateFromSnapshot : BaseUpdateFromSnapshotSystem<Velocity.SnapshotData, Velocity>
        {
        }
    }
}