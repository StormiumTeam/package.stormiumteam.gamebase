using Unity.Collections;
using Unity.Entities;

namespace StormiumTeam.GameBase.Data
{
	public struct ForceMapData : IComponentData
	{
		public FixedString512 NewKey;
	}
}