using EcsComponents.MasterServer;
using Revolution;
using StormiumTeam.GameBase.EcsComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	[BurstCompile]
	public unsafe struct ClientLoadedRpc : IRpcCommand
	{
		public class RequestSystem : RpcCommandRequestSystem<ClientLoadedRpc>
		{
		}

		public int            GameVersion;
		public ulong          MasterServerUserId;
		public NativeString64 ConnectionToken;

		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(GameVersion);
			writer.Write(MasterServerUserId);
			writer.Write(ConnectionToken.LengthInBytes);
			fixed (byte* buffer = &ConnectionToken.buffer.byte0000)
			{
				for (var i = 0; i != ConnectionToken.LengthInBytes; i++)
				{
					writer.Write(buffer[i]);
				}
			}
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			var byteRead = reader.GetBytesRead(ref ctx);
			if (reader.Length - byteRead < sizeof(int))
			{
				GameVersion = -1;
				return;
			}

			GameVersion                   = reader.ReadInt(ref ctx);
			MasterServerUserId            = reader.ReadULong(ref ctx);
			ConnectionToken.LengthInBytes = reader.ReadUShort(ref ctx);
			fixed (byte* buffer = &ConnectionToken.buffer.byte0000)
			{
				for (var i = 0; i != ConnectionToken.LengthInBytes; i++)
				{
					buffer[i] = reader.ReadByte(ref ctx);
				}
			}
		}

		[BurstCompile]
		private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
		{
			RpcExecutor.ExecuteCreateRequestComponent<ClientLoadedRpc>(ref parameters);
		}

		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class CreateGamePlayerSystem : JobComponentSystem
	{
		private BeginSimulationEntityCommandBufferSystem m_Barrier;
		private EndSimulationEntityCommandBufferSystem   m_EndBarrier;
		private EntityQuery                              m_PreviousEventQuery;

		public NativeHashMap<ulong, NativeString64> TokenMap;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier            = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			m_EndBarrier         = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
			m_PreviousEventQuery = GetEntityQuery(typeof(PlayerConnectedEvent));

			TokenMap = new NativeHashMap<ulong, NativeString64>(1, Allocator.Persistent);

			GetEntityQuery(typeof(ClientLoadedRpc));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var peLength = m_PreviousEventQuery.CalculateEntityCount();
			if (peLength > 0) m_EndBarrier.CreateCommandBuffer().DestroyEntity(m_PreviousEventQuery);

			inputDeps = new CreateJob
			{
				CurrentVersion      = GameStatic.Version,
				CommandBuffer       = m_Barrier.CreateCommandBuffer().ToConcurrent(),
				PlayerArchetype     = World.GetOrCreateSystem<GamePlayerProvider>().EntityArchetype,
				NetworkIdFromEntity = GetComponentDataFromEntity<NetworkIdComponent>(),

				HasMasterServerConnection = HasSingleton<ConnectedMasterServerClient>(),
				UserIdToToken             = TokenMap
			}.ScheduleSingle(this, inputDeps);

			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (TokenMap.IsCreated)
				TokenMap.Dispose();
		}

		private struct CreateJob : IJobForEachWithEntity<ClientLoadedRpc, ReceiveRpcCommandRequestComponent>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;

			public EntityArchetype PlayerArchetype;

			public int CurrentVersion;

			[ReadOnly]
			public ComponentDataFromEntity<NetworkIdComponent> NetworkIdFromEntity;

			public bool                                 HasMasterServerConnection;
			public NativeHashMap<ulong, NativeString64> UserIdToToken;

			public void Execute(Entity entity, int jobIndex, ref ClientLoadedRpc create, [ReadOnly] ref ReceiveRpcCommandRequestComponent receive)
			{
				CommandBuffer.DestroyEntity(jobIndex, entity);

				if (create.GameVersion != CurrentVersion)
				{
					Debug.Log($"bye bye [player version:{create.GameVersion}, current: {CurrentVersion}]");
					CommandBuffer.AddComponent(jobIndex, receive.SourceConnection, new NetworkStreamRequestDisconnect {Reason = NetworkStreamDisconnectReason.ConnectionClose});
					return;
				}

				if (HasMasterServerConnection)
				{
					if (!UserIdToToken.TryGetValue(create.MasterServerUserId, out var tokenFromMasterServer))
					{
						Debug.LogError("No token found for " + create.MasterServerUserId);
						CommandBuffer.AddComponent(jobIndex, receive.SourceConnection, new NetworkStreamRequestDisconnect {Reason = NetworkStreamDisconnectReason.ConnectionClose});
						return;
					}

					if (!tokenFromMasterServer.Equals(create.ConnectionToken))
					{
						Debug.LogError($"Invalid token (id={create.MasterServerUserId}, curr={create.ConnectionToken.ToString()}, req={tokenFromMasterServer.ToString()})");
						CommandBuffer.AddComponent(jobIndex, receive.SourceConnection, new NetworkStreamRequestDisconnect {Reason = NetworkStreamDisconnectReason.ConnectionClose});
						return;
					}
				}

				var networkId = NetworkIdFromEntity[receive.SourceConnection];

				var geEnt = CommandBuffer.CreateEntity(jobIndex, PlayerArchetype);
				CommandBuffer.SetComponent(jobIndex, geEnt, new GamePlayer(0) {ServerId = networkId.Value, MasterServerId = create.MasterServerUserId});
				CommandBuffer.AddComponent(jobIndex, geEnt, new NetworkOwner {Value     = receive.SourceConnection});
				CommandBuffer.AddComponent(jobIndex, geEnt, new GamePlayerReadyTag());
				CommandBuffer.AddComponent(jobIndex, geEnt, new GhostEntity());
				CommandBuffer.AddComponent(jobIndex, geEnt, new WorldOwnedTag());

				Debug.Log($"Create GamePlayer {geEnt}; source={receive.SourceConnection}");

				CommandBuffer.SetComponent(jobIndex, receive.SourceConnection, new CommandTargetComponent {targetEntity = geEnt});

				// Create event
				var evEnt = CommandBuffer.CreateEntity(jobIndex);
				CommandBuffer.AddComponent(jobIndex, evEnt, new PlayerConnectedEvent {Player = geEnt, Connection = receive.SourceConnection, ServerId = networkId.Value});

				var reqEnt = CommandBuffer.CreateEntity(jobIndex);
				CommandBuffer.AddComponent(jobIndex, reqEnt, new PlayerConnectedRpc {ServerId = networkId.Value});
				CommandBuffer.AddComponent(jobIndex, reqEnt, new SendRpcCommandRequestComponent()); // send to everyone
			}
		}
	}
}