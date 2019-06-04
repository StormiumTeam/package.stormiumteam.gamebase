using Unity.Entities;
using Unity.Mathematics;

namespace StormiumTeam.GameBase
{
    public struct Velocity : IComponentData
    {
        public float3 Value;

        public float3 normalized => math.normalizesafe(Value);
        public float  speed      => math.length(Value);
        public float  speedSqr   => math.lengthsq(Value);

        public Velocity(float3 value)
        {
            Value = value;
        }

        public class Streamer : SnapshotEntityDataAutomaticStreamer<Velocity>
        {
        }
    }
}