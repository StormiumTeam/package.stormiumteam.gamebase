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
					Collider        = (Collider*) rigidBody.Collider.GetUnsafePtr(),
					RigidBodyIndex  = rigidBodyIndex,
					Target          = rigidBody.Entity,
					WorldFromMotion = rigidBody.WorldFromBody
				};
			}

			return array;
		}
	}
}