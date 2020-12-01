using System;
using System.Net;
using GameHost.Transports.enet;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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

		private const int MaxSimulationFrames = 4;

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

			using var packets = new NativeList<Packet>(Allocator.Temp);
			using var dispose = new NativeList<Packet>(Allocator.Temp);
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
									if (receivedFrames++ >= MaxSimulationFrames)
										break;

									packets.Add(netEvent.Packet);

									break;
								default:
									throw new ArgumentOutOfRangeException();
							}

							dispose.Add(netEvent.Packet);
							break;
						case NetEventType.Timeout:
							Debug.Log("timeout");
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}

			for (var i = 0; i < packets.Length; i++)
			{
				var reader               = new DataBufferReader(packets[i].Data + sizeof(int), packets[i].Length);
				var simulationDataReader = new DataBufferReader(reader, reader.CurrReadIndex, reader.Length);

				// A packet is only worth if it's either the first packet or the last one
				Dependency = JobHandle.CombineDependencies(Dependency,
					World.GetExistingSystem<ReceiveSimulationWorldSystem>()
					     .OnNewMessage(ref simulationDataReader, i == 0 || i == packets.Length - 1));

				if (i == 0)
					World.GetExistingSystem<ReceiveFirstFrameGhSimulationSystemGroup>().ForceUpdate();
				World.GetExistingSystem<ReceiveGhSimulationSystemGroup>().ForceUpdate();
			}

			foreach (var d in dispose)
				d.Dispose();

			if (receivedFrames > 0)
				World.GetExistingSystem<ReceiveLastFrameGhSimulationSystemGroup>().ForceUpdate();
			
			Dependency.Complete();
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