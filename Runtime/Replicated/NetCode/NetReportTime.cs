using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.InterTick;
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
		}
	}
}