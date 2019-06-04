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

    public struct GamePlayerToNetworkClient : IComponentData
    {
        /// <summary>
        /// This variable should not be synced between connections and need to be assigned locally.
        /// This hold a target to the server client entity.
        /// </summary>
        public Entity Target;

        public GamePlayerToNetworkClient(Entity target)
        {
            Target = target;
        }
    }

    public struct NetworkClientToGamePlayer : IComponentData
    {
        public Entity Target;

        public NetworkClientToGamePlayer(Entity target)
        {
            Target = target;
        }
    }

    public class GamePlayerProvider : SystemProvider
    {
        protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
        {
            return EntityManager.CreateEntity
            (
                ComponentType.ReadWrite<PlayerDescription>(),
                ComponentType.ReadWrite<GamePlayer>(),
                ComponentType.ReadWrite<ModelIdent>(),
                ComponentType.ReadWrite<GenerateEntitySnapshot>()
            );
        }

        protected override void DestroyEntity(Entity worldEntity)
        {
            // should we also destroy attached modules?
            EntityManager.DestroyEntity(worldEntity);
        }
    }
}