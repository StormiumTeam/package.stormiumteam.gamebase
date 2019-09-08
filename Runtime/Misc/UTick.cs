using System;
using Revolution.NetCode;
using Unity.Mathematics;

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

		public long   Value;
		public float Delta;

		public double Seconds => Value * (double) Delta;
		public int    Ms      => (int) (Seconds * 1000);

		public uint AsUInt
		{
			get
			{
				#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (Value > uint.MaxValue)
					throw new ArgumentOutOfRangeException($"<getAsUInt> Current Tick Value ({Value}) is bigger than {uint.MaxValue}");
				#endif

				return (uint) Value;
			}
		}

		public int AsInt
		{
			get
			{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (Value > uint.MaxValue)
					throw new ArgumentOutOfRangeException($"<getAsInt> Current Tick Value ({Value}) is bigger than {int.MaxValue}");
#endif

				return (int) Value;
			}
		}

		public static UTick AddTick(UTick tick, long value)
		{
			tick.Value += value;
			return tick;
		}
		
		public static UTick AddTick(UTick tick, UTick other)
		{
			tick.Value += other.Value;
			return tick;
		}

		public void AddMs(uint arg)
		{
			Value += (int) (arg * Delta);
		}

		/// <summary>
		/// Add milliseconds as ticks, if it's not modified, add another tick.
		/// </summary>
		/// <param name="arg"></param>
		public void AddMsNextFrame(long arg)
		{
			var val = (int) (arg * Delta);
			Value = math.select(Value + 1, Value + val, val > 0);
		}

		public static UTick MsToTick(in UTick reference, long ms)
		{
			UTick tick;
			tick.Value = (int) (ms * reference.Delta);
			tick.Delta = reference.Delta;
			return tick;
		}

		public static UTick MsToTickNextFrame(in UTick reference, long ms)
		{
			var   val = (int) (ms * reference.Delta);
			UTick tick;
			tick.Value = math.select(1, val, val > 0);
			tick.Delta = reference.Delta;

			return tick;
		}

		public static UTick AddMs(UTick tick, long arg)
		{
			tick.Value += (int) (arg * tick.Delta);
			return tick;
		}
		
		public static UTick AddMsNextFrame(UTick tick, long arg)
		{
			var val = (int) (arg * tick.Delta);
			tick.Value = math.select(tick.Value + 1, tick.Value + val, val > 0);
			return tick;
		}
		
		public static UTick CopyDelta(UTick reference, long newTick)
		{
			reference.Value = (int) newTick;
			return reference;
		}
	}

	public static class UTickExtensions
	{
		public static UTick GetTick(this ServerSimulationSystemGroup system)
		{
			UTick tick;
			tick.Value = (int) system.ServerTick;
			tick.Delta = FixedTimeLoop.fixedTimeStep;
			return tick;
		}

		public static UTick GetTickInterpolated(this NetworkTimeSystem system)
		{
			UTick tick;
			tick.Value = (int) system.interpolateTargetTick;
			tick.Delta = FixedTimeLoop.fixedTimeStep;
			return tick;
		}

		public static UTick GetTickPredicted(this NetworkTimeSystem system)
		{
			UTick tick;
			tick.Value = (int) system.predictTargetTick;
			tick.Delta = FixedTimeLoop.fixedTimeStep;
			return tick;
		}
	}
}