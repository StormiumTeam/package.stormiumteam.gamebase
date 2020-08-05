using StormiumTeam.GameBase.BaseSystems.Interfaces;
using StormiumTeam.GameBase._Camera;
using StormiumTeam.GameBase.Utility.Rendering;
using Unity.Entities;

namespace StormiumTeam.GameBase.BaseSystems.Ext
{
	public static class GameBaseSystemExtensions
	{
		public static Entity GetFirstSelfGamePlayer<TSystem>(this TSystem system)
			where TSystem : ComponentSystemBase, IGameBaseSystem
		{
			var query = system.GetLocalPlayerGroup();
			if (query.CalculateEntityCount() > 0)
				return query.GetSingletonEntity();

			return default;
		}
		
		public static ComputedCameraState GetComputedCameraState<TSystem>(this TSystem system)
			where TSystem : ComponentSystemBase, IGameBaseSystem
		{
			return system.EntityManager.GetComponentData<ComputedCameraState>(system.GetSingletonEntity<DefaultCamera>());
		}
	}
}