using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace StormiumTeam.GameBase.Components
{
	// was called 'TargetBumpEvent' before.
	public struct TargetImpulseEvent : IComponentData, IEventData
	{
		public Entity Origin;
		public Entity Destination;

		/// <summary>
		/// The position of the impulsion origin
		/// </summary>
		public float3 Position;

		/// <summary>
		/// The impulse force
		/// </summary>
		public float3 Force;

		/// <summary>
		/// How much velocity from the destination should we keep? (range: [0-1], where 0 is no velocity kept)
		/// </summary>
		public float3 Momentum;

		public class Provider : BaseProviderBatch<TargetImpulseEvent>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(GameEvent),
					typeof(TargetImpulseEvent),
				};
			}

			public override void SetEntityData(Entity entity, TargetImpulseEvent data)
			{
				EntityManager.SetComponentData(entity, data);
			}

			protected override void OnUpdate()
			{
				EntityManager.DestroyEntity(Entities.WithAll<GameEvent, TargetImpulseEvent>().ToEntityQuery());
				
				base.OnUpdate();
			}
		}
	}
}