using package.stormiumteam.shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace StormiumTeam.GameBase
{
	[InternalBufferCapacity(1)]
	public unsafe struct CollideWith : IBufferElementData
	{
		public int RigidBodyIndex;

		public CollideWith(int rigidBodyIndex)
		{
			RigidBodyIndex = rigidBodyIndex;
		}

		public static UnsafeAllocationLength<CustomCollide> ToCustomCollideArray(DynamicBuffer<CollideWith> cwBuffer, NativeSlice<RigidBody> rigidBodies, Allocator allocator = Allocator.Temp)
		{
			var array = new UnsafeAllocationLength<CustomCollide>(allocator, cwBuffer.Length);

			for (var i = 0; i != cwBuffer.Length; i++)
			{
				var rigidBodyIndex = cwBuffer[i].RigidBodyIndex;
				var rigidBody      = rigidBodies[rigidBodyIndex];

				array[i] = new CustomCollide
				{
					Collider        = rigidBody.Collider,
					RigidBodyIndex  = rigidBodyIndex,
					Target          = rigidBody.Entity,
					WorldFromMotion = rigidBody.WorldFromBody
				};
			}

			return array;
		}

		public static void UpdateFilterRecursion(Collider* collider, CollisionFilter filter)
		{
			while (true)
			{
				// we need to force the change
				((BoxCollider*) collider)->Filter = filter;
				if (collider->CollisionType == CollisionType.Composite)
				{
					var key = new ColliderKey();
					if (collider->GetChild(ref key, out var child))
					{
						collider = child.Collider;
						continue;
					}
				}

				break;
			}
		}

		public static void Set(DynamicBuffer<CollideWith> cwBuffer, PhysicsWorld physicsWorld)
		{
			var count = cwBuffer.Length;
			for (var i = 0; i != count; i++)
			{
				var rigidBodyIndex = cwBuffer[i].RigidBodyIndex;
				var collider       = physicsWorld.Bodies[rigidBodyIndex].Collider;
				var filter         = collider->Filter;

				MainBit.SetBitAt(ref filter.BelongsTo, 31, true);

				UpdateFilterRecursion(collider, filter);
			}
		}

		public static void Release(DynamicBuffer<CollideWith> cwBuffer, PhysicsWorld physicsWorld)
		{
			var count = cwBuffer.Length;
			for (var i = 0; i != count; i++)
			{
				var rigidBodyIndex = cwBuffer[i].RigidBodyIndex;
				var collider       = physicsWorld.Bodies[rigidBodyIndex].Collider;
				var filter         = collider->Filter;

				MainBit.SetBitAt(ref filter.BelongsTo, 31, false);

				UpdateFilterRecursion(collider, filter);
			}
		}
	}
}