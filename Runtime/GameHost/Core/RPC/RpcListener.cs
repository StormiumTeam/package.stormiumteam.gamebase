using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using GameHost.Core.IO;
using GameHost.Native;
using LiteNetLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevolutionSnapshot.Core.Buffers;
using UnityEngine;

namespace GameHost.Core.RPC
{
	public class RpcListener : INetEventListener
	{
		private LiteNetLibRpcClient client;
		
		public RpcListener(LiteNetLibRpcClient client)
		{
			this.client = client;
		}

		public virtual void OnPeerConnected(NetPeer peer)
		{
			Console.WriteLine("connected");
			Connected?.Invoke();
		}

		public virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
		{
		}

		public virtual void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
		{
		}

		public virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
		{
			Console.WriteLine("received data");

			var str      = reader.GetString();
			var document = JObject.Parse(str);

			var jsonRpcProperty = document["jsonrpc"];
			if (jsonRpcProperty is null)
				throw new InvalidOperationException("no jsonrpc property");

			var methodProperty = document["method"];
			if (methodProperty is null && document["error"] is null)
				throw new InvalidOperationException("no method property");

			var resultProperty = document["result"];
			var idProperty     = document["id"];
			var paramsProperty = document["params"];
			var errorProperty  = document["error"];
	
			Debug.Assert(jsonRpcProperty.ToObject<string>() == "2.0", "jsonRpcProperty.ToObject<string>() == '2.0'");
			Debug.LogError(jsonRpcProperty.ToObject<string>());

			switch (resultProperty != null)
			{
				case true when paramsProperty != null:
					throw new InvalidOperationException("can't be a request and a response at the same time");
				case true when idProperty == null:
					throw new InvalidOperationException("follow-up required but no id present");
			}

			if (resultProperty != null)
			{
				client.AddResponse(methodProperty, resultProperty, idProperty);
			}
			else if (errorProperty == null)
			{
				client.AddRequestOrNotification(methodProperty, paramsProperty, idProperty);
			}
			else
			{
				client.AddError(errorProperty, idProperty);
			}
		}

		public virtual void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
		{
		}

		public virtual void OnNetworkLatencyUpdate(NetPeer peer, int latency)
		{
		}

		public virtual void OnConnectionRequest(ConnectionRequest request)
		{
			Console.WriteLine("accept");
			request.Accept();
		}

		public event Action Connected;
	}
}