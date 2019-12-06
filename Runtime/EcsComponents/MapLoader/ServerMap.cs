using System.Net.Mime;
using Unity.NetCode;
using StormiumTeam.GameBase.EcsComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;

namespace StormiumTeam.GameBase.Data
{
	public struct ExecutingServerMap : IComponentData
	{
		public NativeString512 Key;
	}

	[BurstCompile]
	public unsafe struct UpdateServerMapRpc : IRpcCommand
	{
		public class RequestSystem : RpcCommandRequestSystem<UpdateServerMapRpc>
		{}
		
		public NativeString512 Key;

		public void Serialize(DataStreamWriter writer)
		{
			writer.WritePackedUInt((uint) Key.LengthInBytes, NetworkCompressionModel);

			var prevKey = default(ushort);
			for (int i = 0, length = Key.LengthInBytes; i != length; i++)
			{
				var curr = UnsafeUtility.ReadArrayElement<ushort>(UnsafeUtility.AddressOf(ref Key.buffer), i);
				writer.WritePackedUIntDelta(curr, prevKey, NetworkCompressionModel);
				prevKey = curr;
			}

			writer.Flush();
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			var length = reader.ReadPackedUInt(ref ctx, NetworkCompressionModel);
			Key = new NativeString512 {LengthInBytes = (ushort) length};

			var prevKey = default(ushort);
			for (var i = 0; i != length; i++)
			{
				var curr = (ushort) reader.ReadPackedUIntDelta(ref ctx, prevKey, NetworkCompressionModel);
				UnsafeUtility.WriteArrayElement(UnsafeUtility.AddressOf(ref Key.buffer), i, curr);
				prevKey = curr;
			}
		}

		[BurstCompile]
		private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
		{
			RpcExecutor.ExecuteCreateRequestComponent<UpdateServerMapRpc>(ref parameters);
		}

		private static readonly NetworkCompressionModel NetworkCompressionModel;

		static UpdateServerMapRpc()
		{
			NetworkCompressionModel = new NetworkCompressionModel(Allocator.Persistent);
			Application.quitting += DoQuit;
		}
		
		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
		}

		private static void DoQuit()
		{
			NetworkCompressionModel.Dispose();
			Application.quitting -= DoQuit;
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