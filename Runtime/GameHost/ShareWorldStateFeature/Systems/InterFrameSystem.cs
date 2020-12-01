using GameHost.Simulation.Utility.InterTick;
using StormiumTeam.GameBase.Time.Components;
using Unity.Entities;

namespace GameHost.ShareSimuWorldFeature.Systems
{
	[UpdateBefore(typeof(BeforeFirstFrameGhSimulationSystemGroup))]
	public class ResetInterFrame : SystemBase
	{
		protected override void OnUpdate()
		{
			if (HasSingleton<InterFrame>())
				SetSingleton(new InterFrame());
		}
	}

	[UpdateInGroup(typeof(ReceiveFirstFrameGhSimulationSystemGroup))]
	public class InterFrameSystemBegin : SystemBase
	{
		protected override void OnCreate()
		{
			base.OnCreate();
			RequireSingletonForUpdate<GameTime>();
		}

		protected override void OnUpdate()
		{
			if (!HasSingleton<InterFrame>())
			{
				EntityManager.CreateEntity(typeof(InterFrame));
			}

			var gameTime = GetSingleton<GameTime>();
			SetSingleton(new InterFrame
			{
				Begin = gameTime,
				End   = gameTime,
				Range = new RangeTick((uint) gameTime.Frame, (uint) gameTime.Frame)
			});
		}
	}

	[UpdateInGroup(typeof(ReceiveGhSimulationSystemGroup))]
	public class InterFrameSystem : SystemBase
	{
		protected override void OnCreate()
		{
			base.OnCreate();
			RequireSingletonForUpdate<GameTime>();
			RequireSingletonForUpdate<InterFrame>();
		}

		protected override void OnUpdate()
		{
			var previous = GetSingleton<InterFrame>();
			previous.End       = GetSingleton<GameTime>();
			previous.Range.End = (uint) previous.End.Frame;
			SetSingleton(previous);
		}
	}

	public struct InterFrame : IComponentData
	{
		public GameTime Begin;
		public GameTime End;

		public RangeTick Range;
	}
}