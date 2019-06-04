using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Extensions;

namespace Runtime.Systems.Filters
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	public class FilterLivables : CollisionFilterSystemBase
	{
		public override string Name => "Livables filter rule.";
		public override string Description => "Automatically add Livables colliders to collision physics filters";

		[BurstCompile]
		private struct Job : IJobForEachWithEntity<PhysicsCollider, Relative<LivableDescription>>, IFilter
		{
			public void Execute(Entity entity, int index, ref PhysicsCollider c0, ref Relative<LivableDescription> owner)
			{
				var rigidBodyIndex = PhysicsWorld.GetRigidBodyIndex(entity);

				for (var i = 0; i != Targets.Length; i++)
				{
					CollideWithFromEntity[Targets[i]].Add(new CollideWith(rigidBodyIndex));
				}
			}

			[field: NativeDisableParallelForRestriction]
			public BufferFromEntity<CollideWith> CollideWithFromEntity { get; set; }

			[field: NativeDisableParallelForRestriction]
			public NativeArray<Entity> Targets { get; set; }

			[field: ReadOnly]
			public PhysicsWorld PhysicsWorld { get; set; }
		}

		public override JobHandle Filter(PhysicsWorld physicsWorld, NativeArray<Entity> targets, JobHandle jobHandle)
		{
			return FillVariables(new Job()).Schedule(this, jobHandle);
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return inputDeps;
		}
	}
}