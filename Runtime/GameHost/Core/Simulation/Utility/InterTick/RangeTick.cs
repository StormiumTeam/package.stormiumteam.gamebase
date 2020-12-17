﻿using System;

namespace GameHost.Simulation.Utility.InterTick
{
	public struct RangeTick
	{
		public uint Begin;
		public uint End;

		public RangeTick(uint begin, uint end)
		{
			Begin = begin;
			End   = Math.Max(Begin, end);
		}

		public int Length
		{
			get { return (int) (End - Begin); }
			set
			{
#if DEBUG
				if (value < 0)
					throw new ArgumentOutOfRangeException();
#endif
				End = (uint) Math.Max(Begin, Begin + value);

			}
		}

		public bool Contains(long tick)
		{
			if (tick == default)
				return false;
				
			return End >= tick && tick >= Begin;
		}

		public bool Contains(UTick tick)
		{
			if (tick.Value == default)
				return false;
			
			return End >= tick.Value && tick.Value >= Begin;
		}
		
		public override string ToString()
		{
			return $"({Begin}, {End} ({Length}))";
		}
	}
}