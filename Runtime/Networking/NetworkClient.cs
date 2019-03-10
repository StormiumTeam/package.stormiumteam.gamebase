using Unity.Entities;

namespace StormiumTeam.GameBase.Networking
{
    public struct NetworkClient : IComponentData
    {
        public long Id;
    }

    public struct NetworkLocalTag : IComponentData
    {
    }
} 