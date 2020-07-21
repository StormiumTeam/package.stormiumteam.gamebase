using System;
using GameHost.Core.IO;
using Unity.Entities;

namespace GameHost.InputBackendFeature.Components
{
	public struct InputAction
	{
		public int Id;
	}

	public struct ReplicatedInputAction : IComponentData, IEquatable<ReplicatedInputAction>
	{
		public TransportConnection Connection;
		public int                 Id;

		public bool Equals(ReplicatedInputAction other)
		{
			return Connection.Equals(other.Connection) && Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is ReplicatedInputAction other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Connection.GetHashCode() * 397) ^ Id;
			}
		}
	}
}