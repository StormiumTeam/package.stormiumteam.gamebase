using GameHost.Core.IO;
using GameHost.Transports.enet;

namespace GameHost.Transports.Transports.ENet
{
	public class ENetTransportAddress : TransportAddress
	{
		public readonly Address Address;

		public ENetTransportAddress(Address address)
		{
			Address = address;
		}

		public override TransportDriver Connect()
		{
			var driver = new ENetTransportDriver(1);
			driver.Connect(Address);
			return driver;
		}
	}
}