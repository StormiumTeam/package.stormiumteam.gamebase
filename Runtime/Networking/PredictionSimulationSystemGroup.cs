using Revolution;
using Revolution.NetCode;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	public class PredictionSimulationSystemGroup : ComponentSystemGroup
	{
		private bool                        m_IsServer;
		private ServerSimulationSystemGroup m_ServerSimulationSystemGroup;
		private ClientSimulationSystemGroup m_ClientSimulationSystemGroup;
		private SnapshotReceiveSystem       m_SnapshotReceiveSystem;
		private NetworkTimeSystem           m_TimeSystem;

		public uint  SimulatedTick   { get; private set; }
		public uint  DestinationTick { get; private set; }
		public float DeltaTime       { get; private set; }

		protected override void OnCreate()
		{
			base.OnCreate();

			m_IsServer = (m_ServerSimulationSystemGroup = World.GetExistingSystem<ServerSimulationSystemGroup>()) != null;
			if (!m_IsServer)
			{
				m_TimeSystem                  = World.GetOrCreateSystem<NetworkTimeSystem>();
				m_ClientSimulationSystemGroup = World.GetOrCreateSystem<ClientSimulationSystemGroup>();
				m_SnapshotReceiveSystem       = World.GetOrCreateSystem<SnapshotReceiveSystem>();
			}
		}

		protected override void OnUpdate()
		{
			var start = 0u;
			var end   = 0u;

			if (m_IsServer)
			{
				start     = m_ServerSimulationSystemGroup.ServerTick;
				end       = start + 1;
				DeltaTime = m_ServerSimulationSystemGroup.UpdateDeltaTime;
			}
			else
			{
				start     = m_SnapshotReceiveSystem.JobData.Tick;
				end       = m_TimeSystem.predictTargetTick - 1;
				DeltaTime = m_ClientSimulationSystemGroup.UpdateDeltaTime;
			}

			if (start == 0 || end == 0)
			{
				Debug.Log($"[{(m_IsServer ? "Server" : "Client")}] not running group, tick='{start}'; end='{end}'");
				return;
			}

			if (end > start + 50 && true == false)
			{
				Debug.LogWarning("Excessing frames...");
			}
			end             = math.min(start + 25, end);
			DestinationTick = end;

			for (var i = start; i < end; i++)
			{
				SimulatedTick = i;
				base.OnUpdate();
			}
		}
	}
}