using System;
using System.Net;
using GameHost.Core.IO;
using GameHost.Core.RPC;
using GameHost.Native;
using LiteNetLib;
using LiteNetLib.Utils;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;

namespace GameHost.Core
{
	[AlwaysUpdateSystem]
	public class GameHostConnector : AbsGameBaseSystem
	{
		public event Action Connected;
		
		private RpcListener listener;
		
		public  NetManager  Client;

		protected override void OnCreate()
		{
			base.OnCreate();
			Client = new NetManager(listener = new RpcListener(World.GetExistingSystem<RpcCollectionSystem>()));

			listener.Connected += () =>
			{
				Console.WriteLine("Reiter");
				Connected?.Invoke();
			};
		}

		protected override void OnUpdate()
		{
			if (Client.IsRunning)
				Client.PollEvents();
		}

		public void Connect(IPEndPoint ep)
		{
			Client.Start();
			Client.Connect(ep, "GAMEHOST.CLIENT.V1");
		}

		public void BroadcastRequest(CharBuffer128 command, DataBufferWriter data)
		{
			Console.WriteLine("send data!");
			
			var writer = new NetDataWriter(true);
			writer.Put(nameof(RpcMessageType.Command));
			writer.Put(nameof(RpcCommandType.Send));
			writer.Put(command.ToString());
			if (data.IsCreated)
				writer.Put(data.Span.ToArray());

			Client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
		}

		public void SendReply(TransportConnection connection, CharBuffer128 command, DataBufferWriter data)
		{
			var peer = Client.GetPeerById((int) connection.Id);
			if (peer == null)
				throw new InvalidOperationException($"Peer '{connection.Id}' not existing");

			var writer = new NetDataWriter(true, data.Length);
			writer.Put(nameof(RpcMessageType.Command));
			writer.Put(nameof(RpcCommandType.Reply));
			writer.Put(command.ToString());
			writer.Put(data.Span.ToArray());

			peer.Send(writer, DeliveryMethod.ReliableOrdered);
		}
	}
}