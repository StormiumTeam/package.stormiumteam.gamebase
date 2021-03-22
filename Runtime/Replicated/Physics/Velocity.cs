using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;
using Unity.Mathematics;

namespace PataNext.Module.Simulation.GameBase.Physics.Components
{
	public struct Velocity : IComponentData
	{
		public float3 Value;
	}
}