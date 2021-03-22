using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace GameHost.Revolution.NetCode.Components
{
	public struct LocalInstigatorId : IComponentData
	{
		public int Value;

		public class Register : RegisterGameHostComponentData<LocalInstigatorId>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<LocalInstigatorId>();
		}
	}
}