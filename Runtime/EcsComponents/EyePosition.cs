using Unity.Entities;
using Unity.Mathematics;

namespace Stormium.Core
{
	public struct EyePosition : IComponentData
	{
		public float3 Value;

		public EyePosition(float3 value)
		{
			Value = value;
		}
	}
}