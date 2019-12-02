using Unity.NetCode;
using StormiumTeam.GameBase.EcsComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase.Data
{
	public struct ExecutingServerMap : IComponentData
	{
		public NativeString512 Key;
	}

	public unsafe struct UpdateServerMapRpc : IRpcCommand
	{
		public NativeString512 Key;

		public void Serialize(DataStreamWriter writer)
		{
			using (var compression = new NetworkCompressionModel(Allocator.Temp))
			{
				writer.WritePackedUInt((uint) Key.LengthInBytes, compression);
				for (int i = 0, length = Key.LengthInBytes; i != length; i++)
				{
					writer.WritePackedUInt(UnsafeUtility.ReadArrayElement<ushort>(UnsafeUtility.AddressOf(ref Key.buffer), i), compression);
				}
			}

			writer.Flush();
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			using (var compression = new NetworkCompressionModel(Allocator.Temp))
			{
				var length = reader.ReadPackedUInt(ref ctx, compression);
				Key = new NativeString512 {LengthInBytes = (ushort) length};
				for (var i = 0; i != length; i++)
				{
					UnsafeUtility.WriteArrayElement(UnsafeUtility.AddressOf(ref Key.buffer), i, (ushort) reader.ReadPackedUInt(ref ctx, compression));
				}
			}
		}

		[BurstCompile]
		private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
		{
			RpcExecutor.ExecuteCreateRequestComponent<UpdateServerMapRpc>(ref parameters);
		}

		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
		}

		public Entity SourceConnection { get; set; }
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

		private Entity m_PreviousMapEntity;

		protected override void OnCreate()
		{
			base.OnCreate();

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
					var requestEnt = EntityManager.CreateEntity(typeof(UpdateServerMapRpc), typeof(SendRpcCommandRequestComponent));
					EntityManager.SetComponentData(requestEnt, new UpdateServerMapRpc
					{
						Key = m_ExecutingMapQuery.GetSingleton<ExecutingMapData>().Key
					});
					EntityManager.SetComponentData(requestEnt, new SendRpcCommandRequestComponent
					{
						TargetConnection = netOwner.Value
					});

					// We don't directly add the component here... we delay it for next update
					PostUpdateCommands.AddComponent(entity, ComponentType.ReadWrite<ClientSynchronized>());
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
				var requestEnt = EntityManager.CreateEntity(typeof(UpdateServerMapRpc), typeof(SendRpcCommandRequestComponent));
				EntityManager.SetComponentData(requestEnt, new UpdateServerMapRpc
				{
					Key = m_ExecutingMapQuery.GetSingleton<ExecutingMapData>().Key
				});
				EntityManager.SetComponentData(requestEnt, new SendRpcCommandRequestComponent
				{
					TargetConnection = netOwner.Value
				});
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

			m_RpcQuery       = GetEntityQuery(typeof(UpdateServerMapRpc));
			m_ServerMapQuery = GetEntityQuery(typeof(ExecutingServerMap));
		}

		protected override void OnUpdate()
		{
			if (m_RpcQuery.CalculateEntityCount() == 0)
				return;

			// Destroy previous one...
			EntityManager.DestroyEntity(m_ServerMapQuery);
			EntityManager.CreateEntity(typeof(ExecutingServerMap));

			Entities.With(m_RpcQuery).ForEach((Entity reqEntity, ref UpdateServerMapRpc req) => { SetSingleton(new ExecutingServerMap {Key = req.Key}); });

			// Destroy current query
			EntityManager.DestroyEntity(m_RpcQuery);
		}
	}
}