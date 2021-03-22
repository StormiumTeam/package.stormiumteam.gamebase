using GameHost.Revolution.NetCode.Components;
using GameHost.ShareSimuWorldFeature.Systems;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Time.Components;
using Unity.Entities;

namespace StormiumTeam.GameBase.Systems
{
	public class TimeSystem : AbsGameBaseSystem
	{
		protected override void OnUpdate()
		{
		}

		public NetReportTime GetReport(Entity entity)
		{
			if (HasComponent<NetReportTime>(entity))
				return GetComponent<NetReportTime>(entity);

			if (!HasSingleton<InterFrame>())
				return new NetReportTime {Continuous = 1};

			var interFrame = GetSingleton<InterFrame>();
			if (interFrame.Call == 0)
			{
				var gameTime = GetSingleton<GameTime>();
				return new NetReportTime
				{
					Begin      = gameTime,
					End        = gameTime,
					Continuous = 1
				};
			}

			return new NetReportTime
			{
				Begin      = interFrame.Begin,
				End        = interFrame.End,
				Continuous = 0
			};
		}
	}
}