using System;
using Unity.Entities;

namespace GameHost.Simulation.Utility.Resource.Components
{
	public struct GameResourceKey<T> : IComponentData
		where T : struct, IEquatable<T>, IGameResourceKeyDescription
	{
		public T Value;
	}

	public interface IGameResourceKeyDescription
	{
	}
}