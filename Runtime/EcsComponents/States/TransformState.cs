using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.quaternion;

namespace StormiumTeam.GameBase
{
    public struct TransformState : IStateData, IComponentData
    {
        public float3 Position;
        public quaternion Rotation;

        public TransformState(InterpolationBuffer buffer) : this(buffer.Position, buffer.Rotation)
        {
        }

        public TransformState(half3 position, half3 rotation)
        {
            Position = position;
            Rotation = Euler(rotation);
        }

        public TransformState(float3 position, quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    public enum Dir : byte
    {
        ConvertToState = 0,
        ConvertFromState = 1,
        Both = 2
    }

    public struct TransformStateDirection : IComponentData
    {   
        public Dir Value;

        public TransformStateDirection(Dir value)
        {
            Value = value;
        }
    }

    public struct InterpolationData : IComponentData
    {
        public Entity Instance;
        public int Lock1, Lock2;
    }

    public struct InterpolationBuffer : IBufferElementData
    {
        public int Tick;
        public int SnapshotIdx;
        
        public float3     Position;
        public quaternion Rotation;

        public InterpolationBuffer(TransformState from, int snapshotIdx, int tick)
        {
            Position = from.Position;
            Rotation = from.Rotation;

            Tick = tick;
            SnapshotIdx = snapshotIdx;
        }
    }
}