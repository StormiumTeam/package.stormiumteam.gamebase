using Revolution;
using Unity.NetCode;
using Revolution.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase
{
    public struct Velocity : IComponentData
    {
        public struct Exclude : IComponentData
        {
        }

        public struct SnapshotData : IReadWriteSnapshot<SnapshotData>, ISynchronizeImpl<Velocity>
        {
            public const int   Quantization   = 100;
            public const float DeQuantization = 1 / 100f;

            public uint Tick { get; set; }

            public QuantizedFloat3 Velocity; // float * 1000

            public void WriteTo(DataStreamWriter writer, ref SnapshotData baseline, NetworkCompressionModel compressionModel)
            {
                for (var i = 0; i != 3; i++)
                    writer.WritePackedIntDelta(Velocity[i], baseline.Velocity[i], compressionModel);
            }

            public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref SnapshotData baseline, NetworkCompressionModel compressionModel)
            {
                for (var i = 0; i != 3; i++)
                    Velocity[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Velocity[i], compressionModel);
            }

            public void SynchronizeFrom(in Velocity component, in DefaultSetup setup, in SerializeClientData serializeData)
            {
                Velocity.Set(Quantization, component.Value);
            }

            public void SynchronizeTo(ref Velocity component, in DeserializeClientData deserializeData)
            {
                component.Value = Velocity.Get(DeQuantization);
            }
        }

        public float3 Value;

        public float3 normalized => math.normalizesafe(Value);
        public float  speed      => math.length(Value);
        public float  speedSqr   => math.lengthsq(Value);
        
        public float3 xfz => new float3(Value.x, 0, Value.z); 

        public Velocity(float3 value)
        {
            Value = value;
        }

        public class System : ComponentSnapshotSystem_Basic<Velocity, SnapshotData>
        {
            public override ComponentType ExcludeComponent => typeof(Exclude);
        }

        public class Synchronize : ComponentUpdateSystemDirect<Velocity, SnapshotData>
        {
        }
    }
}