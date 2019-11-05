using StormiumTeam.GameBase;

namespace  StormiumTeam.GameBase
{
	public struct UTimeProgression
	{
		private float m_Accumulated;
		public  int   Value;

		public void Reset()
		{
			m_Accumulated = 0.0f;
			Value = 0;
		}

		public float GetAccumulated() => m_Accumulated;

		public static UTimeProgression operator +(UTimeProgression left, UTick tick)
		{
			var delta   = tick.Delta;
			var deltaMs = tick.DeltaMs;

			left.m_Accumulated += delta * 1000;
			while (left.m_Accumulated > deltaMs)
			{
				left.Value         += deltaMs;
				left.m_Accumulated -= deltaMs;
			}

			return left;
		}
	}

}