using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace GameHost.Simulation.Utility.Resource.Components
{
	public struct IsResourceEntity : IComponentData
	{
		public class Register : RegisterGameHostComponentData<IsResourceEntity>
		{
		}
	}
}