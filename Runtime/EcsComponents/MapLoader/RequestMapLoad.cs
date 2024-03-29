using Unity.Collections;
using Unity.Entities;

namespace StormiumTeam.GameBase.Data
{
	/// <summary>
	///     Request the load of a map.
	/// </summary>
	public struct RequestMapLoad : IComponentData
	{
		public FixedString512 Key;
	}
}