using Unity.Mathematics;

namespace Stormium.Core.Projectiles
{
	public static class ProjectileUtility
	{
		public static float3 Project(in float3 position, ref float3 velocity, float dt, float3 gravity = default)
		{
			float3 target;
			target = position + velocity * dt;
			velocity += gravity * dt;

			return target;
		}
	}
}