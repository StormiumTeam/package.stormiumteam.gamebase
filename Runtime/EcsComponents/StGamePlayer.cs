using System;
using package.stormiumteam.networking.runtime.lowlevel;
using Stormium.Core;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Runtime.Data
{
    public struct StGamePlayer : IComponentData
    {
        public struct WritePayload : IWriteEntityDataPayload<StGamePlayer>
        {
            public ComponentDataFromEntity<StGamePlayerToNetworkClient> ToNetworkClients;

            public void Write(int index, Entity entity, ComponentDataFromEntity<StGamePlayer> stateFromEntity, ComponentDataFromEntity<DataChanged<StGamePlayer>> changeFromEntity, DataBufferWriter data, SnapshotReceiver receiver, StSnapshotRuntime runtime)
            {
                data.WriteUnmanaged(stateFromEntity[entity]);
                if (ToNetworkClients.Exists(entity))
                {
                    // Is the user owned from the same client? (1 = yes, 0 = no)
                    data.WriteByte((byte) math.select(0, 1, ToNetworkClients[entity].Target == receiver.Client));
                }
                else
                {
                    data.WriteByte(0);
                }
            }
        }

        public struct ReadPayload : IReadEntityDataPayload<StGamePlayer>
        {
            public void Read(int index, Entity entity, ComponentDataFromEntity<StGamePlayer> dataFromEntity, ref DataBufferReader data, SnapshotSender sender, StSnapshotRuntime runtime)
            {
                var player = data.ReadValue<StGamePlayer>();
                player.IsSelf = data.ReadValue<byte>();

                dataFromEntity[entity] = player;
            }
        } 
        
        public class Streamer : SnapshotEntityDataManualStreamer<StGamePlayer, WritePayload, ReadPayload>
        {
            protected override void UpdatePayloadW(ref WritePayload current)
            {
                current.ToNetworkClients = GetComponentDataFromEntity<StGamePlayerToNetworkClient>();
            }

            protected override void UpdatePayloadR(ref ReadPayload current)
            {
            }
        }

        public ulong MasterServerId;
        public byte  IsSelf;

        public StGamePlayer(ulong masterServerId, bool isSelf)
        {
            MasterServerId = masterServerId;
            IsSelf        = isSelf ? (byte) 1 : (byte) 0;
        }
    }

    public struct StGamePlayerToNetworkClient : IComponentData
    {
        /// <summary>
        /// This variable should not be synced between connections and need to be assigned locally.
        /// This hold a target to the server client entity.
        /// </summary>
        public Entity Target;

        public StGamePlayerToNetworkClient(Entity target)
        {
            Target = target;
        }
    }

    public struct StNetworkClientToGamePlayer : IComponentData
    {
        public Entity Target;

        public StNetworkClientToGamePlayer(Entity target)
        {
            Target = target;
        }
    }

    public class StGamePlayerProvider : SystemProvider
    {
        protected override Entity SpawnEntity(Entity origin, StSnapshotRuntime snapshotRuntime)
        {
            return EntityManager.CreateEntity
            (
                ComponentType.ReadWrite<PlayerDescription>(),
                ComponentType.ReadWrite<StGamePlayer>(),
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