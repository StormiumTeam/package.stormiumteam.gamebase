using GameHost.Core.IO;
using GameHost.Transports.enet;

namespace GameHost.Transports.Transports.ENet
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