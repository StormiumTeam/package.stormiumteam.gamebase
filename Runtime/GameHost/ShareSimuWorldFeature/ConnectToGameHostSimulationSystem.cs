using System;
using System.Net;
using ENet;
using RevolutionSnapshot.Core.Buffers;
using Unity.Entities;
using UnityEngine;
using EventType = ENet.EventType;

namespace DefaultNamespace
{
	public class ConnectToGameHostSimulationSystem : SystemBase
	{
		const int maxTries         = 4;
		const int maxEventPerFrame = 32;

		private Host m_Host;
		private Peer m_Peer;

		private float m_NextPingDelay;
		private int   m_ConnectionRetryCount;

		private void ConnectionLost(bool forcefullyTerminated)
		{
			var str = $"Connection Lost to <{m_Peer.IP}:{m_Peer.Port}>! - ";
			if (forcefullyTerminated)
				str += "Terminating";
			else
				str += $"New Try ({m_ConnectionRetryCount}/{maxTries})";

			Debug.LogError(str);
		}

		protected override void OnUpdate()
		{
			if (m_Host == null)
				return;

			if (m_Peer.State == PeerState.Disconnected)
			{
				if (m_ConnectionRetryCount > maxTries)
				{
					m_Host.Dispose();
					m_Host = null;
					ConnectionLost(true);
					return;
				}

				Connect(new IPEndPoint(IPAddress.Parse(m_Peer.IP), m_Peer.Port));

				m_ConnectionRetryCount++;
				ConnectionLost(false);
				return;
			}

			m_NextPingDelay -= Time.DeltaTime;
			if (m_NextPingDelay < 0)
			{
				m_NextPingDelay = 2.0f;
				m_Peer.Ping();
			}

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
						case EventType.None:
							break;
						case EventType.Connect:
							Debug.Log("connection!");
							break;
						case EventType.Disconnect:
							break;
						case EventType.Receive:
							Debug.Log("on receive!");
							var reader = new DataBufferReader(netEvent.Packet.Data, netEvent.Packet.Length);
							World.GetExistingSystem<ReceiveSimulationWorldSystem>()
							     .OnNewMessage(ref reader);
							netEvent.Packet.Dispose();
							break;
						case EventType.Timeout:
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
			m_Peer.Disconnect(0);
			m_Host.Service(0, out _);
			m_Host.Dispose();
		}

		public bool Connect(IPEndPoint endPoint)
		{
			var addr = new Address();
			addr.SetIP(endPoint.Address.ToString());
			addr.Port = (ushort) endPoint.Port;

			m_Host = new Host();
			m_Host.Create();
			m_Peer = m_Host.Connect(addr);
			if (!m_Peer.IsSet)
				Debug.LogWarning("Couldn't connect to " + endPoint);

			return m_Peer.IsSet;
		}
	}
}