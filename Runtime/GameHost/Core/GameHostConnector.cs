using System;
using System.Collections.Generic;
using System.Net;
using GameHost.Core.IO;
using GameHost.Core.RPC;
using GameHost.Core.RPC.Interfaces;
using GameHost.Native;
using LiteNetLib;
using LiteNetLib.Utils;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;
using UnityEngine.Events;

namespace GameHost.Core
{
	[AlwaysUpdateSystem]
	public class GameHostConnector : AbsGameBaseSystem
	{
		public event Action Connected;
		public RpcClient    RpcClient   { get; }
		public bool         IsConnected { get; private set; }

		public GameHostConnector()
		{
			var client = new LiteNetLibRpcClient();
			
			client.SubscribeConnected(onConnected);

			RpcClient = client;
		}

		private void onConnected()
		{
			Console.WriteLine("onConnected");
			
			IsConnected = true;
			Connected?.Invoke();
		}

		protected override void OnUpdate()
		{
			RpcClient.Poll();
		}

		public void Connect(IPEndPoint ep)
		{
			IsConnected = false;
			(RpcClient as LiteNetLibRpcClient).ConnectTo(ep);
		}

		private Dictionary<Type, UnityEventBase> notificationToUEventMap = new Dictionary<Type, UnityEventBase>();

		public UnityEvent<T> GetNotificationEvent<T>()
			where T : IGameHostRpcPacket, new()
		{
			if (!notificationToUEventMap.ContainsKey(typeof(T)))
			{
				var uEvent = new UnityEvent<T>();
				RpcClient.SubscribeNotification<T>(notification => { uEvent.Invoke(notification); });

				notificationToUEventMap[typeof(T)] = uEvent;

				return uEvent;
			}

			return notificationToUEventMap[typeof(T)] as UnityEvent<T>;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			
			foreach (var uevent in notificationToUEventMap)
				uevent.Value.RemoveAllListeners();

			notificationToUEventMap.Clear();
			notificationToUEventMap = null;
		}
	}
}