using System;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public struct UTick : IComparable<UTick>
	{
		public int CompareTo(UTick other)
		{
			return Value.CompareTo(other.Value);
		}

		#region Operators

		public static bool operator ==(UTick left, UTick right)
		{
			return left.Value == right.Value;
		}

		public static bool operator ==(UTick left, uint right)
		{
			return left.Value == right;
		}

		public static bool operator ==(UTick left, int right)
		{
			return left.Value == right;
		}

		public static bool operator ==(UTick left, long right)
		{
			return left.Value == right;
		}

		public static bool operator !=(UTick left, UTick right)
		{
			return left.Value != right.Value;
		}

		public static bool operator !=(UTick left, uint right)
		{
			return left.Value == right;
		}

		public static bool operator !=(UTick left, int right)
		{
			return left.Value == right;
		}

		public static bool operator !=(UTick left, long right)
		{
			return left.Value == right;
		}

		public static bool operator <(UTick left, UTick right)
		{
			return left.Value < right.Value;
		}

		public static bool operator <(UTick left, uint right)
		{
			return left.Value < right;
		}

		public static bool operator <(UTick left, int right)
		{
			return left.Value < right;
		}

		public static bool operator <(UTick left, long right)
		{
			return left.Value < right;
		}

		public static bool operator >(UTick left, UTick right)
		{
			return left.Value > right.Value;
		}

		public static bool operator >(UTick left, uint right)
		{
			return left.Value > right;
		}

		public static bool operator >(UTick left, int right)
		{
			return left.Value > right;
		}

		public static bool operator >(UTick left, long right)
		{
			return left.Value > right;
		}

		public static bool operator <=(UTick left, UTick right)
		{
			return left.Value <= right.Value;
		}

		public static bool operator <=(UTick left, uint right)
		{
			return left.Value <= right;
		}

		public static bool operator <=(UTick left, int right)
		{
			return left.Value <= right;
		}

		public static bool operator <=(UTick left, long right)
		{
			return left.Value <= right;
		}

		public static bool operator >=(UTick left, UTick right)
		{
			return left.Value >= right.Value;
		}

		public static bool operator >=(UTick left, uint right)
		{
			return left.Value >= right;
		}

		public static bool operator >=(UTick left, int right)
		{
			return left.Value >= right;
		}

		public static bool operator >=(UTick left, long right)
		{
			return left.Value >= right;
		}

		#endregion

		public uint  Value;
		public float Delta;

		public double Seconds => Value * Delta;
		public uint   Ms      => (uint) (Seconds * 1000);

		public void AddMs(uint arg)
		{
			Value += (uint) (arg * Delta);
		}

		/// <summary>
		/// Add milliseconds as ticks, if it's not modified, add another tick.
		/// </summary>
		/// <param name="arg"></param>
		public void AddMsNextFrame(uint arg)
		{
			var val = (uint) (arg * Delta);
			Value = math.select(Value + 1, Value + val, val > 0);
		}

		public static UTick MsToTick(in UTick reference, uint ms)
		{
			UTick tick;
			tick.Value = (uint) (ms * reference.Delta);
			tick.Delta = reference.Delta;
			return tick;
		}

		public static UTick MsToTickNextFrame(in UTick reference, uint ms)
		{
			var   val = (uint) (ms * reference.Delta);
			UTick tick;
			tick.Value = math.select(1, val, val > 0);
			tick.Delta = reference.Delta;

			return tick;
		}

		public static UTick AddMs(UTick tick, uint arg)
		{
			tick.Value += (uint) (arg * tick.Delta);
			return tick;
		}

		public static UTick AddMsNextFrame(UTick tick, uint arg)
		{
			var val = (uint) (arg * tick.Delta);
			tick.Value = math.select(tick.Value + 1, tick.Value + val, val > 0);
			return tick;
		}

		public static UTick CopyDelta(UTick reference, uint newTick)
		{
			reference.Value = newTick;
			return reference;
		}
	}

	public static class UTickExtensions
	{
		public static UTick GetTick(this ServerSimulationSystemGroup system)
		{
			UTick tick;
			tick.Value = system.ServerTick;
			tick.Delta = FixedTimeLoop.fixedTimeStep;
			return tick;
		}

		public static UTick GetTickInterpolated(this NetworkTimeSystem system)
		{
			UTick tick;
			tick.Value = system.interpolateTargetTick;
			tick.Delta = FixedTimeLoop.fixedTimeStep;
			return tick;
		}

		public static UTick GetTickPredicted(this NetworkTimeSystem system)
		{
			UTick tick;
			tick.Value = system.predictTargetTick;
			tick.Delta = FixedTimeLoop.fixedTimeStep;
			return tick;
		}
	}
}