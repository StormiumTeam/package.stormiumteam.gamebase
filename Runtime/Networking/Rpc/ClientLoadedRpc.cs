using ENet;
using Revolution;
using Revolution.NetCode;
using StormiumTeam.GameBase.EcsComponents;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;
using UnityEngine;
using Valve.Sockets;

namespace StormiumTeam.GameBase
{
	public struct ClientLoadedRpc : IRpcCommandRequestComponentData
	{
		public int GameVersion;

		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(GameVersion);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			var byteRead = reader.GetBytesRead(ref ctx);
			if (reader.Length - byteRead < sizeof(int))
			{
				GameVersion = -1;
				return;
			}

			GameVersion = reader.ReadInt(ref ctx);
		}

		public Entity SourceConnection { get; set; }
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities))]
	public class CreateGamePlayerSystem : JobComponentSystem
	{
		private struct CreateJob : IJobForEachWithEntity<ClientLoadedRpc>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;

			public EntityArchetype PlayerArchetype;

			public int CurrentVersion;

			public ENetDriver Driver;

			[ReadOnly]
			public ComponentDataFromEntity<NetworkIdComponent> NetworkIdFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<NetworkStreamConnection> NetworkStreamConnectionFromEntity;

			public void Execute(Entity entity, int jobIndex, ref ClientLoadedRpc create)
			{
				CommandBuffer.DestroyEntity(jobIndex, entity);

				if (create.GameVersion != CurrentVersion)
				{
					Debug.Log($"bye bye [player version:{create.GameVersion}, current: {CurrentVersion}]");
					NetworkStreamConnectionFromEntity[create.SourceConnection].Value.Disconnect(Driver);
					return;
				}

				var networkId = NetworkIdFromEntity[create.SourceConnection];

				var geEnt = CommandBuffer.CreateEntity(jobIndex, PlayerArchetype);
				CommandBuffer.SetComponent(jobIndex, geEnt, new GamePlayer(0) {ServerId = networkId.Value});
				CommandBuffer.AddComponent(jobIndex, geEnt, new NetworkOwner {Value     = create.SourceConnection});
				CommandBuffer.AddComponent(jobIndex, geEnt, new GamePlayerReadyTag());
				CommandBuffer.AddComponent(jobIndex, geEnt, new GhostEntity());
				CommandBuffer.AddComponent(jobIndex, geEnt, new WorldOwnedTag());

				Debug.Log($"Create GamePlayer {geEnt}; source={create.SourceConnection}");
				
				CommandBuffer.SetComponent(jobIndex, create.SourceConnection, new CommandTargetComponent {targetEntity = geEnt});
				
				// Create event
				var evEnt = CommandBuffer.CreateEntity(jobIndex);
				CommandBuffer.AddComponent(jobIndex, evEnt, new PlayerConnectedEvent {Player = geEnt, Connection = create.SourceConnection, ServerId = networkId.Value});

				var reqEnt = CommandBuffer.CreateEntity(jobIndex);
				CommandBuffer.AddComponent(jobIndex, reqEnt, new PlayerConnectedRpc {ServerId = networkId.Value});
				CommandBuffer.AddComponent(jobIndex, reqEnt, new SendRpcCommandRequestComponent()); // send to everyone
			}
		}

		private BeginSimulationEntityCommandBufferSystem m_Barrier;
		private EntityQuery                              m_PreviousEventQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier            = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			m_PreviousEventQuery = GetEntityQuery(typeof(PlayerConnectedEvent));

			GetEntityQuery(typeof(ClientLoadedRpc));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var peLength = m_PreviousEventQuery.CalculateEntityCount();
			if (peLength > 0)
			{
				EntityManager.DestroyEntity(m_PreviousEventQuery);
			}
			
			inputDeps = new CreateJob
			{
				CurrentVersion = GameStatic.Version,

				Driver                            = World.GetExistingSystem<NetworkStreamReceiveENetDriver>().Driver,
				NetworkStreamConnectionFromEntity = GetComponentDataFromEntity<NetworkStreamConnection>(true),

				CommandBuffer       = m_Barrier.CreateCommandBuffer().ToConcurrent(),
				PlayerArchetype     = World.GetOrCreateSystem<GamePlayerProvider>().EntityArchetype,
				NetworkIdFromEntity = GetComponentDataFromEntity<NetworkIdComponent>()
			}.ScheduleSingle(this, inputDeps);

			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}