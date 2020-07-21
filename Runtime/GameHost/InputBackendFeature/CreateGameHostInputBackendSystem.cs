using System;
using DefaultNamespace;
using ENet;
using RevolutionSnapshot.Core.Buffers;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.GameHost.Simulation.InputBackendFeature
{
	public class CreateGameHostInputBackendSystem : SystemBase
	{
		const int maxEventPerFrame = 32;

		private Host m_Host;
		private Peer m_Peer;

		protected override void OnCreate()
		{
			base.OnCreate();
		}

		protected override void OnUpdate()
		{
			if (!m_Host.IsSet)
				return;

			for (var i = 0; i != maxEventPerFrame; i++)
			{
				var polled    = false;
				var breakThis = false;
				while (!polled)
				{
					if (m_Host.CheckEvents(out var netEvent) <= 0)
					{
						if (m_Host.Service(0, out netEvent) <= 0)
						{
							break;
						}

						polled = true;
					}

					switch (netEvent.Type)
					{
						case NetEventType.Connect:
							Debug.Log("input connection!");
							break;
						case NetEventType.Receive:
							var reader = new DataBufferReader(netEvent.Packet.Data, netEvent.Packet.Length);
							var type   = (EMessageType) reader.ReadValue<int>();
							switch (type)
							{
								case EMessageType.InputData:
									var inputDataReader = new DataBufferReader(reader, reader.CurrReadIndex, reader.Length);
									break;
								default:
									throw new ArgumentOutOfRangeException();
							}

							netEvent.Packet.Dispose();
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				if (breakThis)
					break;
			}
		}

		protected override void OnDestroy()
		{
			if (m_Peer.IsSet)
				m_Peer.Disconnect(0);
			if (m_Host.IsSet)
			{
				m_Host.Service(0, out _);
				m_Host.Dispose();
			}
		}

		public bool Create(ushort port)
		{
			var addr = new Address {Port = port};

			if (m_Host.IsSet)
				m_Host.Dispose();
			m_Host = new Host();
			m_Host.Create(addr, 32, 1);

			return m_Host.IsSet;
		}
	}
}