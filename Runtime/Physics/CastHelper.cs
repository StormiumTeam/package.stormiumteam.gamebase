using Unity.Mathematics;
using Unity.Physics;

namespace StormiumTeam.GameBase
{
	public static unsafe class CastHelper
	{
		public static ColliderCastInput TransformSpace(this ColliderCastInput input, RigidBody rigidBody, out Math.MTransform pWorldFromMotion)
		{
			return TransformSpace(input, rigidBody.WorldFromBody, out pWorldFromMotion);
		}
		
		public static ColliderCastInput TransformSpace(this ColliderCastInput input, RigidBody rigidBody)
		{
			return TransformSpace(input, rigidBody.WorldFromBody, out _);
		}
		
		public static ColliderCastInput TransformSpace(this ColliderCastInput input, RigidTransform worldFromMotion, out Math.MTransform pWorldFromMotion)
		{
			// Transform the input into body space
			pWorldFromMotion = new Math.MTransform(worldFromMotion);
			var bodyFromWorld = Math.Inverse(pWorldFromMotion);

			return new ColliderCastInput
			{
				Collider    = input.Collider,
				Position    = Math.Mul(bodyFromWorld, input.Position),
				Orientation = math.mul(math.inverse(worldFromMotion.rot), input.Orientation),
				Direction   = math.mul(bodyFromWorld.Rotation, input.Direction)
			};
		}

		public static ColliderDistanceInput TransformSpace(this ColliderDistanceInput input, RigidTransform worldFromMotion, out Math.MTransform pWorldFromMotion)
		{
			// Transform the input into body space
			pWorldFromMotion = new Math.MTransform(worldFromMotion);
			var bodyFromWorld = Math.Inverse(pWorldFromMotion);

			return new ColliderDistanceInput
			{
				Collider = input.Collider,
				Transform = new RigidTransform(
					math.mul(math.inverse(worldFromMotion.rot), input.Transform.rot),
					Math.Mul(bodyFromWorld, input.Transform.pos)
				),
				MaxDistance = input.MaxDistance
			};
		}
	}
}