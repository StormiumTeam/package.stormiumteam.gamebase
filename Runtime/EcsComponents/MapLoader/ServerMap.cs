using Revolution.NetCode;
using StormiumTeam.GameBase.EcsComponents;
using StormiumTeam.GameBase.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase.Data
{
	public struct ExecutingServerMap : IComponentData
	{
		public NativeString512 Key;
	}

	public struct CommandSetServerMap : IComponentData
	{
		public NativeString512 Key;
	}

	public struct UpdateServerMapRpc : IRpcCommand
	{
		public NativeString512 Key;

		public void Execute(Entity connection, World world)
		{
			var ent = world.EntityManager.CreateEntity();
			world.EntityManager.AddComponentData(ent, new CommandSetServerMap {Key = Key});
		}

		public void WriteTo(DataStreamWriter writer)
		{
			using (var compression = new NetworkCompressionModel(Allocator.Temp))
			{
				writer.WritePackedUInt((uint) Key.Length, compression);
				for (int i = 0, length = Key.Length; i != length; i++)
				{
					writer.WritePackedUInt((uint) Key[i], compression);
				}
			}

			writer.Flush();
		}

		public void ReadFrom(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			using (var compression = new NetworkCompressionModel(Allocator.Temp))
			{
				var length = reader.ReadPackedUInt(ref ctx, compression);
				Key = new NativeString512 {Length = (int) length};
				for (var i = 0; i != length; i++)
				{
					Key[i] = (char) reader.ReadPackedUInt(ref ctx, compression);
				}
			}
		}
	}

	[UpdateInGroup(typeof(ServerInitializationSystemGroup))]
	public class ServerSynchronizeMap : ComponentSystem
	{
		private struct ClientSynchronized : IComponentData
		{
		}

		private EntityQuery m_ExecutingMapQuery;
		private EntityQuery m_ClientQuery;
		private EntityQuery m_ClientWithoutMapQuery;

		private DefaultRpcProcessSystem<UpdateServerMapRpc> m_UpdateServerMapRpcSystem;
		private MapManager                                  m_MapManager;

		private Entity m_PreviousMapEntity;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_UpdateServerMapRpcSystem = World.GetOrCreateSystem<DefaultRpcProcessSystem<UpdateServerMapRpc>>();
			m_MapManager               = World.GetOrCreateSystem<MapManager>();

			m_ExecutingMapQuery     = GetEntityQuery(typeof(ExecutingMapData));
			m_ClientQuery           = GetEntityQuery(typeof(GamePlayerReadyTag), typeof(GamePlayer), ComponentType.ReadWrite<ClientSynchronized>());
			m_ClientWithoutMapQuery = GetEntityQuery(typeof(GamePlayerReadyTag), typeof(NetworkOwner), ComponentType.Exclude<ClientSynchronized>());
		}

		protected override void OnUpdate()
		{
			var hasChange = false;
			if (m_ExecutingMapQuery.CalculateEntityCount() > 0)
			{
				Entities.With(m_ClientWithoutMapQuery).ForEach((Entity entity, ref NetworkOwner netOwner) =>
				{
					var outgoingRpcData = EntityManager.GetBuffer<OutgoingRpcDataStreamBufferComponent>(netOwner.Value);
					var rpc = new UpdateServerMapRpc
					{
						Key = m_ExecutingMapQuery.GetSingleton<ExecutingMapData>().Key
					};

					var rpcQueue = m_UpdateServerMapRpcSystem.RpcQueue;
					rpcQueue.Schedule(outgoingRpcData, rpc);

					// We don't directly add the component here... we delay it for next update
					PostUpdateCommands.AddComponent(entity, typeof(ClientSynchronized));
				});

				if (m_PreviousMapEntity != m_ExecutingMapQuery.GetSingletonEntity())
				{
					hasChange           = true;
					m_PreviousMapEntity = m_ExecutingMapQuery.GetSingletonEntity();
				}
			}
			else if (m_PreviousMapEntity != default)
			{
				hasChange           = true;
				m_PreviousMapEntity = default;
			}

			if (!hasChange)
				return;

			Entities.With(m_ClientQuery).ForEach((Entity entity, ref NetworkOwner netOwner) =>
			{
				var outgoingRpcData = EntityManager.GetBuffer<OutgoingRpcDataStreamBufferComponent>(netOwner.Value);
				var rpc = new UpdateServerMapRpc
				{
					Key = m_ExecutingMapQuery.GetSingleton<ExecutingMapData>().Key
				};

				var rpcQueue = m_UpdateServerMapRpcSystem.RpcQueue;
				rpcQueue.Schedule(outgoingRpcData, rpc);

				EntityManager.AddComponent(entity, typeof(ClientSynchronized));
			});
		}
	}

	[UpdateInGroup(typeof(ClientInitializationSystemGroup))]
	public class ClientReceiveMapInformation : ComponentSystem
	{
		private EntityQuery m_RpcQuery;
		private EntityQuery m_ServerMapQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_RpcQuery       = GetEntityQuery(typeof(CommandSetServerMap));
			m_ServerMapQuery = GetEntityQuery(typeof(ExecutingServerMap));
		}

		protected override void OnUpdate()
		{
			if (m_RpcQuery.CalculateEntityCount() == 0)
				return;

			// Destroy previous one...
			EntityManager.DestroyEntity(m_ServerMapQuery);
			EntityManager.CreateEntity(typeof(ExecutingServerMap));

			Entities.With(m_RpcQuery).ForEach((ref CommandSetServerMap cmdData) => { SetSingleton(new ExecutingServerMap {Key = cmdData.Key}); });

			// Destroy current query
			EntityManager.DestroyEntity(m_RpcQuery);
		}
	}
}