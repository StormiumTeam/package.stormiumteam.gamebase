using System;
using Unity.Mathematics;
using Unity.NetCode;

namespace StormiumTeam.GameBase
{
	public struct UTick : IComparable<UTick>
	{
		public int CompareTo(UTick other)
		{
			return Value.CompareTo(other.Value);
		}

		#region Operators

		public static UTick operator +(UTick left, UTick right)
		{
			UTick newTick;
			newTick.Value = left.Value + right.Value;
			newTick.Delta = left.Delta;
			return newTick;
		}

		public static UTick operator -(UTick left, UTick right)
		{
			UTick newTick;
			newTick.Value = left.Value - right.Value;
			newTick.Delta = left.Delta;
			return newTick;
		}

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

		public long  Value;
		public float Delta;

		public int DeltaMs => (int) (Delta * 1000);

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

		public UTick AddMs(uint arg)
		{
			return this = AddMs(this, arg);
		}

		/// <summary>
		///     Add milliseconds as ticks, if it's not modified, add another tick.
		/// </summary>
		/// <param name="arg"></param>
		public void AddMsNextFrame(long arg)
		{
			var val = GetIteration(Delta, arg);
			Value = math.select(Value + 1, Value + val, val > 0);
		}

		public static UTick MsToTick(in UTick reference, long ms)
		{
			UTick tick;
			tick.Value = GetIteration(reference.Delta, ms);
			tick.Delta = reference.Delta;
			return tick;
		}

		public static UTick MsToTickNextFrame(in UTick reference, long ms)
		{
			var val = GetIteration(reference.Delta, ms);

			UTick tick;
			tick.Value = math.select(1, val, val > 0);
			tick.Delta = reference.Delta;

			return tick;
		}

		public static UTick AddMs(UTick tick, long arg)
		{
			tick.Value += GetIteration(tick.Delta, arg);
			return tick;
		}

		public static UTick AddMsNextFrame(UTick tick, long arg)
		{
			var val = GetIteration(tick.Delta, arg);
			tick.Value = math.select(tick.Value + 1, tick.Value + val, val > 0);
			return tick;
		}

		public static UTick CopyDelta(UTick reference, long newTick)
		{
			reference.Value = (int) newTick;
			return reference;
		}

		private static int GetIteration(float delta, long ms)
		{
			//return (int) math.round(ms / delta);
			return (int) (ms / (delta * 1000));
		}
	}

	public static class UTickExtensions
	{
		public static UTick GetServerTick(this ServerSimulationSystemGroup system)
		{
			UTick tick;
			tick.Value = system.ServerTick;

			ClientServerTickRate tickRate = default;
			if (system.HasSingleton<ClientServerTickRate>())
				tickRate = system.GetSingleton<ClientServerTickRate>();
			tickRate.ResolveDefaults();

			tick.Delta = 1f / tickRate.SimulationTickRate;

			return tick;
		}

		public static UTick GetServerTick(this ClientSimulationSystemGroup system)
		{
			UTick tick;
			tick.Value = system.ServerTick;
			tick.Delta = system.ServerTickDeltaTime;

			return tick;
		}
		
		public static UTick GetServerInterpolatedTick(this ClientSimulationSystemGroup system)
		{
			UTick tick;
			tick.Value = system.InterpolationTick;
			tick.Delta = system.ServerTickDeltaTime;

			return tick;
		}
	}
}