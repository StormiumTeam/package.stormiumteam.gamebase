using System;
using System.Diagnostics;
using Runtime.BaseSystems;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Runtime.Systems
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	public class CollisionFilterSystemGroup : RuleSystemGroupBase
	{
		public override void Process()
		{
			throw new NotImplementedException("CollisionFilterSystemGroup doesn't implement RuleSystemGroupBase.Process(). Use Filter(query) instead.");
		}

		private EntityQuery m_ResetBufferQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ResetBufferQuery = GetEntityQuery(typeof(CollideWith));
		}

		public JobHandle Filter(EntityQuery query, JobHandle inputDeps)
		{
			using (var entities = query.ToEntityArray(Allocator.TempJob, out var queryDep))
			{
				return Filter(entities, JobHandle.CombineDependencies(inputDeps, queryDep));
			}
		}

		public unsafe JobHandle Filter(NativeArray<Entity> targets, JobHandle inputDeps)
		{
			var handle = inputDeps;
			handle.Complete();

			var physicsWorld = World.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;

			// Reset buffer range
			var entityType          = GetArchetypeChunkEntityType();
			var chunkComponentType0 = GetArchetypeChunkBufferType<CollideWith>();

			// Would there be a way to set 'CollideWith' living outside of chunks automatically?
			using (var ecb = new EntityCommandBuffer(Allocator.TempJob))
			using (var chunks = m_ResetBufferQuery.CreateArchetypeChunkArray(Allocator.TempJob))
			{
				foreach (var chunk in chunks)
				{
					var bufferArray = chunk.GetBufferAccessor(chunkComponentType0);
					var entityArray = (Entity*) chunk.GetNativeArray(entityType).GetUnsafeReadOnlyPtr();

					for (int i = 0, count = chunk.Count; i < count; ++i)
					{
						var buffer = bufferArray[i];
						if (buffer.Capacity <= 10)
						{
							buffer = ecb.SetBuffer<CollideWith>(entityArray[i]);
							buffer.ResizeUninitialized(11);
						}

						buffer.Clear();
					}
				}
				
				ecb.Playback(EntityManager);
			}

			foreach (var system in m_systemsToUpdate)
			{
				var filterSystem = system as CollisionFilterSystemBase;
				Debug.Assert(filterSystem != null, nameof(filterSystem) + " != null");

				handle = filterSystem.InternalStartFiltering(physicsWorld, targets, handle);
			}

			return handle;
		}
	}

	[UpdateInGroup(typeof(CollisionFilterSystemGroup))]
	public abstract class CollisionFilterSystemBase : RuleBaseSystem
	{
		private NativeArray<Entity> m_Targets;
		private PhysicsWorld        m_PhysicsWorld;

		public abstract JobHandle Filter(PhysicsWorld physicsWorld, NativeArray<Entity> targets, JobHandle jobHandle);

		internal JobHandle InternalStartFiltering(PhysicsWorld physicsWorld, NativeArray<Entity> targets, JobHandle handle)
		{
			m_Targets      = targets;
			m_PhysicsWorld = physicsWorld;

			return Enabled
				? Filter(physicsWorld, targets, handle)
				: handle;
		}

		protected TFilterJob FillVariables<TFilterJob>(TFilterJob job)
			where TFilterJob : IFilter
		{
			job.CollideWithFromEntity = GetBufferFromEntity<CollideWith>();
			job.Targets               = m_Targets;
			job.PhysicsWorld          = m_PhysicsWorld;

			return job;
		}
	}
}