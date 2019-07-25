using Unity.Entities;

namespace StormiumTeam.GameBase.Components
{
	public struct TargetDamageEvent : IComponentData, IEventData
	{
		public Entity Origin;
		public Entity Destination;
		public int    Damage;
		
		public class Provider : BaseProviderBatch<TargetDamageEvent>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(GameEvent),
					typeof(TargetDamageEvent),
				};
			}

			public override void SetEntityData(Entity entity, TargetDamageEvent data)
			{
				EntityManager.SetComponentData(entity, data);
			}
		}
	}
}