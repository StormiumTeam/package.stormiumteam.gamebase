using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;

namespace StormiumTeam.GameBase
{
    public struct Velocity : IComponentData
    {
        public float3 Value;

        public Velocity(float3 value)
        {
            Value = value;
        }
        
        public class Streamer : SnapshotEntityDataAutomaticStreamer<Velocity>
        {}
    }
}