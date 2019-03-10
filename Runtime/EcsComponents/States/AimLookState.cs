using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;

namespace Stormium.Core
{
    public struct AimLookState : IStateData, IComponentData
    {
        public float2 Aim;

        public AimLookState(float2 aim)
        {
            Aim = aim;
        }
    }

    public class AimLookStateStreamerBase : SnapshotEntityDataAutomaticStreamer<AimLookState>
    {
        
    }
}