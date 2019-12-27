using System.Linq;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;

namespace Misc.Extensions
{
	public static class GameBaseSystemExtensions
	{
		public static Entity GetFirstSelfGamePlayer<TSystem>(this TSystem system)
			where TSystem : ComponentSystemBase, IGameBaseSystem
		{
			var query = system.GetPlayerGroup();
			if (query.CalculateEntityCount() > 0)
				return query.GetSingletonEntity();

			return default;
		}

		public static bool TryGetCurrentCameraState<TSystem>(this TSystem system, Entity gamePlayer, out CameraState cameraState)
			where TSystem : ComponentSystemBase, IGameBaseSystem
		{
			cameraState = default;
			if (gamePlayer == default)
				return false;

			var comps = system.EntityManager.GetChunk(gamePlayer).Archetype.GetComponentTypes();
			if (!comps.Contains(ComponentType.ReadWrite<ServerCameraState>()))
				return false;

			var serverCamera = system.EntityManager.GetComponentData<ServerCameraState>(gamePlayer);
			if (serverCamera.Mode == CameraMode.Forced || !comps.Contains(ComponentType.ReadWrite<LocalCameraState>()))
			{
				cameraState = serverCamera.Data;
				return true;
			}

			var localCamera = system.EntityManager.GetComponentData<LocalCameraState>(gamePlayer);
			if (localCamera.Mode == CameraMode.Forced)
			{
				cameraState = localCamera.Data;
				return true;
			}

			cameraState = serverCamera.Data;
			return true;
		}

		public static World GetActiveClientWorld(this ComponentSystemBase system)
		{
#if UNITY_SERVER
			throw new Exeception("GetActiveClientWorld() shouldn't be called on server.");
#else
			foreach (var world in World.AllWorlds)
				if (world.GetExistingSystem<ClientPresentationSystemGroup>()?.Enabled == true)
					return world;

			return null;
#endif
		}
	}
}