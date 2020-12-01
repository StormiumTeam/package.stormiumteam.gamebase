using System;
using GameHost.Simulation.Utility.Resource.Interfaces;

namespace GameHost.Simulation.Utility.Resource
{
	public readonly struct GameResource<T> : IEquatable<GameResource<T>>
		where T : IGameResourceDescription
	{
		public static bool operator ==(GameResource<T> left, GameResource<T> right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(GameResource<T> left, GameResource<T> right)
		{
			return !left.Equals(right);
		}

		public readonly GhGameEntitySafe Entity;

		public GameResource(GhGameEntitySafe target)
		{
			Entity = target;
		}

		public bool Equals(GameResource<T> other)
		{
			return Entity.Equals(other.Entity);
		}

		public override bool Equals(object obj)
		{
			return obj is GameResource<T> other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Entity.GetHashCode();
		}
	}
}