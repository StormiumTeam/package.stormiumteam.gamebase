﻿using ENet;
using GameHost.Core.IO;

namespace GameHost.Transports
{
	public partial class ENetTransportDriver
	{
		private struct DriverEvent
		{
			public TransportEvent.EType Type;
			public int                  StreamOffset;
			public int                  Length;
		}

		private struct SendPacket
		{
			public Packet Packet;
			public Peer   Peer;
			public byte   Channel;
		}
	}
}