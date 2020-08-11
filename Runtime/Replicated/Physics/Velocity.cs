using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;
using Unity.Mathematics;

namespace PataNext.Module.Simulation.GameBase.Physics.Components
{
	public struct Velocity : IComponentData
	{
		public float3 Value;

		public class Register : RegisterGameHostComponentData<Velocity>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<Velocity>();
		}
	}
}