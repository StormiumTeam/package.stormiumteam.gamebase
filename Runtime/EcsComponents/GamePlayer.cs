using package.stormiumteam.networking.runtime.lowlevel;
using Unity.Entities;

namespace StormiumTeam.GameBase
{
    public struct GamePlayer : IComponentData
    {
        public ulong MasterServerId;
        public bool  IsSelf;

        public GamePlayer(ulong masterServerId, bool isSelf)
        {
            MasterServerId = masterServerId;
            IsSelf        = isSelf;
        }
    }

    public class GamePlayerProvider : BaseProvider
    {
        public override void GetComponents(out ComponentType[] entityComponents)
        {
            entityComponents = new[]
            {
                ComponentType.ReadWrite<GamePlayer>()
            };
        }
    }
}