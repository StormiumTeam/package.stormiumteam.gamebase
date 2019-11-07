using Revolution;
using Unity.Entities;

namespace StormiumTeam.GameBase.Components
{
	public struct TargetDamageEvent : IComponentData, IEventData
	{
		public Entity Origin;
		public Entity Destination;
		public int    Damage;

		[UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities.SpawnEvent))]
		public class Provider : BaseProviderBatch<TargetDamageEvent>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(GameEvent),
					typeof(TargetDamageEvent),
					typeof(GhostEntity)
				};
			}

			public override void SetEntityData(Entity entity, TargetDamageEvent data)
			{
				EntityManager.SetComponentData(entity, data);
			}

			protected override void OnUpdate()
			{
				EntityManager.DestroyEntity(Entities.WithAll<GameEvent, TargetDamageEvent>().ToEntityQuery());

				base.OnUpdate();
			}
		}
	}
}