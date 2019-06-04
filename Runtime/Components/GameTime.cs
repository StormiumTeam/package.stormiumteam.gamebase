using Unity.Entities;

namespace StormiumTeam.GameBase
{
	public struct GameTime
	{
		public int    Frame;
		public int    Tick;
		public int    DeltaTick;
		public int    FixedTickPerSecond;
		public double Time;
		public float  DeltaTime;
	}

	public struct GameTimeComponent : IComponentData
	{
		public GameTime Value;

		public int    Frame              => Value.Frame;
		public int    Tick               => Value.Tick;
		public int    DeltaTick          => Value.DeltaTick;
		public int    FixedTickPerSecond => Value.FixedTickPerSecond;
		public double Time               => Value.Time;
		public float  DeltaTime          => Value.DeltaTime;

		public GameTimeComponent(GameTime value)
		{
			Value = value;
		}
	}
}