using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace StormiumTeam.GameBase
{
	public struct ColliderCastEventInput : IComponentData
	{
		public ColliderCastInput Value;
		public Entity Entity;

		public float3 Origin    => Value.Position;
		public float3 Direction => Value.Direction;
	}

	public struct ColliderCastEventHit : IComponentData
	{
		public ColliderCastHit Value;

		public bool DidHit => math.all(Value.SurfaceNormal == float3.zero);

		public float3 Position       => Value.Position;
		public float3 Normal         => Value.SurfaceNormal;
		public float3 RigidBodyIndex => Value.RigidBodyIndex;
	}

	public class ColliderCastEventProvider : BaseProviderBatch<ColliderCastEventProvider.Create>
	{
		public struct Create
		{
			public ColliderCastInput Value;
			public Entity Source;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(ColliderCastEventInput),
				typeof(ColliderCastEventHit)
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, new ColliderCastEventInput {Value = data.Value, Entity = data.Source});
			EntityManager.SetComponentData(entity, new ColliderCastEventHit());
		}
	}
}