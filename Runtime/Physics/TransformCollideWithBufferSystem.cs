using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public unsafe class TransformCollideWithBufferSystem : JobComponentSystem
	{
		//[BurstCompile]
		private struct ProcessJob : IJobParallelFor
		{
			[DeallocateOnJobCompletion]
			[ReadOnly]
			public NativeArray<Entity> Entities;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<CollideWith>            CollideWithFromEntity;
			[ReadOnly]
			public ComponentDataFromEntity<PhysicsCollider> Colliders;
			[ReadOnly]
			public ComponentDataFromEntity<Translation>     Translations;
			[ReadOnly]
			public ComponentDataFromEntity<Rotation>        Rotations;

			[ReadOnly]
			public PhysicsWorld PhysicsWorld; //< only used to get rigidbody index

			public void Execute(int index)
			{
				var entity       = Entities[index];
				var buffer       = CollideWithFromEntity[entity];
				var bufferPtr    = buffer.GetUnsafePtr();
				var bufferLength = buffer.Length;
				for (var i = 0; i != bufferLength; i++)
				{
					ref var cw = ref UnsafeUtilityEx.ArrayElementAsRef<CollideWith>(bufferPtr, i);

					var r               = Rotations[cw.Target];
					var t               = Translations[cw.Target];
					var worldFromEntity = new RigidTransform(r.Value, t.Value);

					cw.Collider  = Colliders[cw.Target].ColliderPtr;
					cw.RigidBodyIndex = PhysicsWorld.GetRigidBodyIndex(cw.Target);
					cw.WorldFromMotion = worldFromEntity;
				}
			}
		}

		private ComponentGroup m_Group;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			m_Group = GetComponentGroup(typeof(CollideWith));
		}

		protected override JobHandle OnUpdate(JobHandle jobHandle)
		{
			m_Group.AddDependency(jobHandle);

			var physicsWorld = World.GetExistingManager<BuildPhysicsWorld>().PhysicsWorld;

			var job = new ProcessJob
			{
				Entities = m_Group.ToEntityArray(Allocator.TempJob, out jobHandle),

				CollideWithFromEntity = GetBufferFromEntity<CollideWith>(),
				Colliders             = GetComponentDataFromEntity<PhysicsCollider>(),
				Translations          = GetComponentDataFromEntity<Translation>(),
				Rotations             = GetComponentDataFromEntity<Rotation>(),
				
				PhysicsWorld = physicsWorld
			};

			jobHandle = job.Schedule(m_Group.CalculateLength(), 8, jobHandle);

			return jobHandle;
		}
		
		public JobHandle ScheduleJob(JobHandle jobHandle)
		{
			m_Group.AddDependency(jobHandle);
			
			var physicsWorld = World.GetExistingManager<BuildPhysicsWorld>().PhysicsWorld;
			
			return new ProcessJob
			{
				Entities = m_Group.ToEntityArray(Allocator.TempJob, out jobHandle),

				CollideWithFromEntity = GetBufferFromEntity<CollideWith>(),
				Colliders             = GetComponentDataFromEntity<PhysicsCollider>(),
				Translations          = GetComponentDataFromEntity<Translation>(),
				Rotations             = GetComponentDataFromEntity<Rotation>(),
				
				PhysicsWorld = physicsWorld
			}.Schedule(m_Group.CalculateLength(), 8, jobHandle);
		}
	}
}