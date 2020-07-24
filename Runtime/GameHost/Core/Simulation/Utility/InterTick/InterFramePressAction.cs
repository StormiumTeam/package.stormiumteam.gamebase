﻿namespace GameHost.Simulation.Utility.InterTick
 {
	 public struct InterFramePressAction : IInterFrameInteraction
	 {
		 public int Pressed;
		 public int Released;

		 public readonly bool HasBeenPressed(RangeTick  range) => range.Contains(Pressed);
		 public readonly bool HasBeenReleased(RangeTick range) => range.Contains(Released);

		 public readonly bool AnyUpdate(RangeTick range)
		 {
			 return range.Contains(Pressed) || range.Contains(Released);
		 }
	 }
 }