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
	public unsafe class TransformCustomCollideBufferSystem : JobComponentSystem
	{
		//[BurstCompile]
		private struct ProcessJob : IJobParallelFor
		{
			[DeallocateOnJobCompletion]
			[ReadOnly]
			public NativeArray<Entity> Entities;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<CustomCollide>            CollideWithFromEntity;
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
					ref var cw = ref UnsafeUtilityEx.ArrayElementAsRef<CustomCollide>(bufferPtr, i);

					var r               = Rotations[cw.Target];
					var t               = Translations[cw.Target];
					var worldFromEntity = new RigidTransform(r.Value, t.Value);

					cw.Collider  = Colliders[cw.Target].ColliderPtr;
					cw.RigidBodyIndex = PhysicsWorld.GetRigidBodyIndex(cw.Target);
					cw.WorldFromMotion = worldFromEntity;
				}
			}
		}

		private EntityQuery m_Group;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Group = GetEntityQuery(typeof(CustomCollide));
		}

		protected override JobHandle OnUpdate(JobHandle jobHandle)
		{
			m_Group.AddDependency(jobHandle);

			var physicsWorld = World.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;

			var job = new ProcessJob
			{
				Entities = m_Group.ToEntityArray(Allocator.TempJob, out jobHandle),

				CollideWithFromEntity = GetBufferFromEntity<CustomCollide>(),
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
			
			var physicsWorld = World.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
			
			return new ProcessJob
			{
				Entities = m_Group.ToEntityArray(Allocator.TempJob, out jobHandle),

				CollideWithFromEntity = GetBufferFromEntity<CustomCollide>(),
				Colliders             = GetComponentDataFromEntity<PhysicsCollider>(),
				Translations          = GetComponentDataFromEntity<Translation>(),
				Rotations             = GetComponentDataFromEntity<Rotation>(),
				
				PhysicsWorld = physicsWorld
			}.Schedule(m_Group.CalculateLength(), 8, jobHandle);
		}
	}
}