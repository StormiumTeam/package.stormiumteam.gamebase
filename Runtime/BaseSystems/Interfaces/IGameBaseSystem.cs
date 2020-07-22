using Unity.Entities;

namespace DefaultNamespace.BaseSystems.Interfaces
{
	public interface IGameBaseSystem
	{
		EntityQuery GetPlayerGroup();
		EntityQuery GetLocalPlayerGroup();
	}
}