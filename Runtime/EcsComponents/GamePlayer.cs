using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;

namespace StormiumTeam.GameBase
{
    public struct GamePlayer : IComponentData
    {
        public struct WritePayload : IWriteEntityDataPayload<GamePlayer>
        {
            public ComponentDataFromEntity<GamePlayerToNetworkClient> ToNetworkClients;

            public void Write(int index, Entity entity, ComponentDataFromEntity<GamePlayer> stateFromEntity, ComponentDataFromEntity<DataChanged<GamePlayer>> changeFromEntity, DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime runtime)
            {
                data.WriteUnmanaged(stateFromEntity[entity]);
                if (ToNetworkClients.Exists(entity))
                {
                    // Is the user owned from the same client? (1 = yes, 0 = no)
                    data.WriteValue(ToNetworkClients[entity].Target == receiver.Client);
                }
                else
                {
                    data.WriteValue(0);
                }
            }
        }

        public struct ReadPayload : IReadEntityDataPayload<GamePlayer>
        {
            public void Read(int index, Entity entity, ComponentDataFromEntity<GamePlayer> dataFromEntity, ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
            {
                var player = data.ReadValue<GamePlayer>();
                player.IsSelf = data.ReadValue<bool>();

                dataFromEntity[entity] = player;
            }
        } 
        
        public class Streamer : SnapshotEntityDataManualStreamer<GamePlayer, WritePayload, ReadPayload>
        {
            protected override void UpdatePayloadW(ref WritePayload current)
            {
                current.ToNetworkClients = GetComponentDataFromEntity<GamePlayerToNetworkClient>();
            }

            protected override void UpdatePayloadR(ref ReadPayload current)
            {
            }
        }

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