using System;
using System.Net;
using System.Net.Sockets;
using GameHost.Core.IO;
using GameHost.Native;
using LiteNetLib;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Core.RPC
{
	public class RpcListener : INetEventListener
	{
		private RpcCollectionSystem collection;

		public RpcListener(RpcCollectionSystem collection)
		{
			this.collection = collection;
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
			
			var type = reader.GetString();
			switch (type)
			{
				case nameof(RpcMessageType.Command):
				{
					var commandType = reader.GetString();
					var commandId   = reader.GetString();

					var response = new GameHostCommandResponse
					{
						Connection = new TransportConnection {Id = (uint) peer.Id, Version = 1},
						Command    = CharBufferUtility.Create<CharBuffer128>(commandId),
						Data       = new DataBufferReader(reader.GetRemainingBytesSegment())
					};
					switch (commandType)
					{
						case nameof(RpcCommandType.Send):
						{
							collection.TriggerCommandRequest(response);
							break;
						}
						case nameof(RpcCommandType.Reply):
						{
							collection.TriggerCommandReply(response);
							break;
						}
					}

					break;
				}
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