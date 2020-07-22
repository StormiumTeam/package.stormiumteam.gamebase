using Unity.Entities;

namespace StormiumTeam.GameBase.BaseSystems.Interfaces
{
	public interface IGameBaseSystem
	{
		EntityQuery GetPlayerGroup();
		EntityQuery GetLocalPlayerGroup();
	}
}