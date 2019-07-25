using Unity.Entities;

namespace StormiumTeam.GameBase.Components
{
	public struct TargetDamageEvent : IComponentData, IEventData
	{
		public Entity Origin;
		public Entity Destination;
		public int    Damage;
	}
}