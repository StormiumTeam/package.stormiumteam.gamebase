using System;
using Unity.Entities;

namespace StormiumTeam.GameBase.Bootstraping
{
	public class BootstrapComponent : IComponentData
	{
		public string Name;
	}

	public struct TargetBootstrap : ISharedComponentData, IEquatable<TargetBootstrap>
	{
		public Entity Value;

		public bool Equals(TargetBootstrap other)
		{
			return Value.Equals(other.Value);
		}

		public override bool Equals(object obj)
		{
			return obj is TargetBootstrap other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}