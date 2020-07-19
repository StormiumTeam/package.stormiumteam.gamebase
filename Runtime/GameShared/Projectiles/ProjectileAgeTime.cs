using StormiumTeam.GameBase;
using Unity.Entities;

namespace Stormium.Core.Projectiles
{
	public struct ProjectileAgeTime : IComponentData
	{
		public int StartMs;
		public int EndMs;
	}
}