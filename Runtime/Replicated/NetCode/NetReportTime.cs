using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.InterTick;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Time.Components;
using Unity.Entities;

namespace GameHost.Revolution.NetCode.Components
{
	public struct NetReportTime : IComponentData
	{
		public GameTime Begin, End;
		
		public RangeTick FrameRange => new RangeTick((uint) (Begin.Frame == 0 ? End.Frame : Begin.Frame), (uint) End.Frame);

		/// <summary>
		/// If continuous is at 0, return <see cref="FrameRange"/>
		/// </summary>
		public RangeTick Active => Continuous == 0 ? FrameRange : default;

		/// <summary>
		/// How much of the same last report has been made?
		/// </summary>
		public uint Continuous;

		public class Register : RegisterGameHostComponentData<NetReportTime>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<NetReportTime>();
		}
		
		[UpdateInGroup(typeof(BeforeFirstFrameGhSimulationSystemGroup))]
		public class ForceContinuousSystem : AbsGameBaseSystem
		{
			protected override void OnUpdate()
			{
				// If GameHost didn't sent to us a Networked Frame, then this mean we can set it to continuous.
				// If it did, it would just reset back to 0...
				Entities.ForEach((ref NetReportTime report) =>
				{
					report.Continuous++;
				}).Schedule();
			}
		}

		[UpdateInGroup(typeof(ReceiveGhSimulationSystemGroup))]
		public class DecreaseOnDifferenceSystem : AbsGameBaseSystem
		{
			protected override void OnUpdate()
			{
				Entities.ForEach((ref NetReportTime report) =>
				{
					report.Continuous--;
				}).Schedule();
			}
		}
	}
}