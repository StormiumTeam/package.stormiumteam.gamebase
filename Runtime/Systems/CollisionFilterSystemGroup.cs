using System;
using System.Diagnostics;
using Revolution.NetCode;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;

namespace StormiumTeam.GameBase.Systems
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	public class CollisionFilterSystemGroup : RuleSystemGroupBase
	{
		private struct DisposeJob : IJob
		{
			[DeallocateOnJobCompletion] public NativeArray<Entity> Entities;

			public void Execute()
			{
			}
		}

		[BurstCompile]
		private struct UpdateAndCleanCollideWithBufferJob : IJobParallelFor
		{
			[NativeDisableParallelForRestriction]
			public BufferFromEntity<CollideWith> CollideWithFromEntity;

			[ReadOnly]
			public NativeArray<Entity> Entities;

			public void Execute(int index)
			{
				var cwBuffer = CollideWithFromEntity[Entities[index]];
				if (cwBuffer.Capacity <= 10)
				{
					cwBuffer.ResizeUninitialized(11);
				}

				cwBuffer.Clear();
			}
		}

		[BurstCompile]
		private struct ClearCollideWithBufferJob : IJobParallelFor
		{
			[NativeDisableParallelForRestriction]
			public BufferFromEntity<CollideWith> CollideWithFromEntity;

			[ReadOnly]
			public NativeArray<Entity> Entities;

			public void Execute(int index)
			{
				var cwBuffer = CollideWithFromEntity[Entities[index]];
				cwBuffer.Clear();
			}
		}

		public override void Process()
		{
			throw new NotImplementedException("CollisionFilterSystemGroup doesn't implement RuleSystemGroupBase.Process(). Use Filter(query) instead.");
		}

		private EntityQuery         m_ResetBufferQuery;
		private GameJobHiddenSystem m_HiddenJobSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ResetBufferQuery = GetEntityQuery(typeof(CollideWith));
			m_HiddenJobSystem  = World.GetOrCreateSystem<GameJobHiddenSystem>();
		}

		public JobHandle Filter(EntityQuery query, JobHandle inputDeps)
		{
			var entities = query.ToEntityArray(Allocator.TempJob, out var queryDep);
			inputDeps = Filter(entities, JobHandle.CombineDependencies(inputDeps, queryDep));
			inputDeps = new DisposeJob
			{
				Entities = entities
			}.Schedule(inputDeps);

			return inputDeps;
		}

		private int m_PreviousBufferVersion;

		public unsafe JobHandle Filter(NativeArray<Entity> targets, JobHandle inputDeps)
		{
			var handle     = inputDeps;
			var newVersion = EntityManager.GetComponentOrderVersion<CollideWith>();
			if (m_PreviousBufferVersion != newVersion)
			{
				handle.Complete();
				handle = new UpdateAndCleanCollideWithBufferJob
				{
					Entities              = targets,
					CollideWithFromEntity = m_HiddenJobSystem.GetBufferFromEntity<CollideWith>()
				}.Schedule(targets.Length, 16, handle);
			}
			else
			{
				handle = new ClearCollideWithBufferJob
				{
					Entities              = targets,
					CollideWithFromEntity = m_HiddenJobSystem.GetBufferFromEntity<CollideWith>()
				}.Schedule(targets.Length, 64, handle);
			}

			var physicsWorld = World.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
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