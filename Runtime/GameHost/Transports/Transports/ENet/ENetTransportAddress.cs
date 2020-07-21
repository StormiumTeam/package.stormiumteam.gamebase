﻿using ENet;
using GameHost.Core.IO;

namespace GameHost.Transports
{
	public class ENetTransportAddress : TransportAddress
	{
		public readonly Address Address;

		public ENetTransportAddress(Address address)
		{
			this.Address = address;
		}

		public override TransportDriver Connect()
		{
			var driver = new ENetTransportDriver(1);
			driver.Connect(Address);
			return driver;
		}
	}
}