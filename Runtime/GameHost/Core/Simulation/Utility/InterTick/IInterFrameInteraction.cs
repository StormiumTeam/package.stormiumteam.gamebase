﻿namespace GameHost.Simulation.Utility.InterTick
{
	public interface IInterFrameInteraction
	{
		bool AnyUpdate(RangeTick range);
	}
}