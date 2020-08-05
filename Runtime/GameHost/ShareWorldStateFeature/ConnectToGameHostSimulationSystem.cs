﻿using System;
using System.Net;
using GameHost.Transports.enet;
using RevolutionSnapshot.Core.Buffers;
using Unity.Entities;
using UnityEngine;

namespace GameHost.ShareSimuWorldFeature
{
	[AlwaysUpdateSystem]
	public class ConnectToGameHostSimulationSystem : SystemBase
	{
		private const int maxTries         = 4;
		private const int maxEventPerFrame = 32;
		private       int m_ConnectionRetryCount;

		private Host m_Host;

		private float m_NextPingDelay;
		private Peer  m_Peer;

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
			World.GetExistingSystem<BeforeFirstFrameGhSimulationSystemGroup>().ForceUpdate();
			
			if (!m_Host.IsSet)
				return;

			if (m_Peer.State == PeerState.Disconnected)
			{
				World.GetExistingSystem<ReceiveSimulationWorldSystem>()
				     .OnDisconnected();

				if (m_ConnectionRetryCount >= maxTries)
				{
					m_Host.Dispose();
					m_Host = default;
					ConnectionLost(true);
					return;
				}

				Connect(new IPEndPoint(IPAddress.Parse(m_Peer.IP), m_Peer.Port));

				m_ConnectionRetryCount++;
				ConnectionLost(false);
				return;
			}

			m_ConnectionRetryCount = 0;

			m_NextPingDelay -= Time.DeltaTime;
			if (m_NextPingDelay < 0)
			{
				m_NextPingDelay = 2.0f;
				m_Peer.Ping();
			}

			var receivedFrames = 0;

			for (var i = 0; i != maxEventPerFrame; i++)
			{
				var polled    = false;
				var breakThis = false;
				while (!polled)
				{
					if (m_Host.CheckEvents(out var netEvent) <= 0)
					{
						if (m_Host.Service(0, out netEvent) <= 0) break;

						polled = true;
					}

					switch (netEvent.Type)
					{
						case NetEventType.None:
							break;
						case NetEventType.Connect:
							Debug.Log("connection!");
							break;
						case NetEventType.Disconnect:
							Debug.Log("disconnection");
							break;
						case NetEventType.Receive:
							var reader = new DataBufferReader(netEvent.Packet.Data, netEvent.Packet.Length);
							var type   = (EMessageType) reader.ReadValue<int>();
							switch (type)
							{
								case EMessageType.Rpc:
									break;
								case EMessageType.SimulationData:
									var simulationDataReader = new DataBufferReader(reader, reader.CurrReadIndex, reader.Length);
									World.GetExistingSystem<ReceiveSimulationWorldSystem>()
									     .OnNewMessage(ref simulationDataReader);

									if (receivedFrames == 0)
										World.GetExistingSystem<ReceiveFirstFrameGhSimulationSystemGroup>().ForceUpdate();
									World.GetExistingSystem<ReceiveGhSimulationSystemGroup>().ForceUpdate();
									receivedFrames++;

									break;
								default:
									throw new ArgumentOutOfRangeException();
							}

							netEvent.Packet.Dispose();
							break;
						case NetEventType.Timeout:
							Debug.Log("timeout");
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				if (breakThis)
					break;
			}

			if (receivedFrames > 0)
				World.GetExistingSystem<ReceiveLastFrameGhSimulationSystemGroup>().ForceUpdate();
		}

		protected override void OnDestroy()
		{
			if (m_Host.IsSet)
			{
				if (m_Peer.IsSet)
					m_Peer.Disconnect(0);
				m_Host.Service(0, out _);
				m_Host.Dispose();
			}
		}

		public bool Connect(IPEndPoint endPoint)
		{
			var addr = new Address();
			addr.SetIP(endPoint.Address.ToString());
			addr.Port = (ushort) endPoint.Port;

			m_Host = new Host();
			if (!m_Host.Create())
				throw new InvalidOperationException();
				
			m_Peer = m_Host.Connect(addr);
			m_Peer.Timeout(0, 4000, 6000);

			if (!m_Peer.IsSet)
				Debug.LogWarning("Couldn't connect to " + endPoint);
			
			Debug.Log("Created Connection Request!");

			return m_Peer.IsSet;
		}
	}
}