using Revolution;
using Unity.Entities;
using Unity.Mathematics;

namespace StormiumTeam.GameBase.Components
{
	// was called 'TargetBumpEvent' before.
	public struct TargetImpulseEvent : IComponentData
	{
		public Entity Origin;
		public Entity Destination;

		/// <summary>
		///     The position of the impulsion origin
		/// </summary>
		public float3 Position;

		/// <summary>
		///     The impulse force
		/// </summary>
		public float3 Force;

		/// <summary>
		///     How much velocity from the destination should we keep? (range: [0-1], where 0 is no velocity kept)
		/// </summary>
		public float3 Momentum;

		[UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities.SpawnEvent))]
		public class Provider : BaseProviderBatch<TargetImpulseEvent>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(GameEvent),
					typeof(TargetImpulseEvent),
					typeof(GhostEntity)
				};
			}

			public override void SetEntityData(Entity entity, TargetImpulseEvent data)
			{
				EntityManager.SetComponentData(entity, data);
			}

			private EntityQuery m_Query;
			protected override void OnUpdate()
			{
				m_Query = m_Query ?? GetEntityQuery(typeof(GameEvent), typeof(TargetImpulseEvent));
				EntityManager.DestroyEntity(m_Query);
				
				base.OnUpdate();
			}
		}
	}
}