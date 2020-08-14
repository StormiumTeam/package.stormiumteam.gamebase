using Unity.Collections;
using Unity.Entities;
using UnityEngine.SceneManagement;

namespace StormiumTeam.GameBase.Data
{
	/// <summary>
	///     The current map (for more information, use MapManager properties)
	/// </summary>
	public struct ExecutingMapData : IComponentData
	{
		public FixedString512 Key;
	}

	public struct MapScene : IBufferElementData
	{
		public Scene Value;
	}
}