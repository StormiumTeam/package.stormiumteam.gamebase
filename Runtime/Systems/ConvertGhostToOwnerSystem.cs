using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Runtime.Systems
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(GhostSpawnSystemGroup))]
	public class ConvertGhostToOwnerSystem : JobComponentSystem
	{
		[BurstCompile]
		private struct ConvertJob : IJobForEach<GhostOwner, Owner>
		{
			[ReadOnly, NativeDisableContainerSafetyRestriction]
			public NativeHashMap<int, GhostEntity> GhostEntityMap;

			public void Execute(ref GhostOwner ghostOwner, ref Owner owner)
			{
				if (ghostOwner.GhostId == 0)
					return;

				if (GhostEntityMap.TryGetValue(ghostOwner.GhostId, out var ghostEntity))
				{
					owner.Target = ghostEntity.entity;
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new ConvertJob
			{
				GhostEntityMap = World.GetExistingSystem<GhostReceiveSystemGroup>().GhostEntityMap
			}.Schedule(this, inputDeps);
		}
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(GhostSpawnSystemGroup))]
	public class ConvertGhostToRelativeSystemGroup : ComponentSystemGroup
	{
	}

	[UpdateInGroup(typeof(ConvertGhostToRelativeSystemGroup))]
	public class ConvertGhostToRelativeSystem<TDescription> : JobComponentSystem
		where TDescription : struct, IEntityDescription
	{
		[BurstCompile]
		private struct ConvertJob : IJobForEach<GhostRelative<TDescription>, Relative<TDescription>>
		{
			[ReadOnly, NativeDisableContainerSafetyRestriction]
			public NativeHashMap<int, GhostEntity> GhostEntityMap;

			public void Execute([ReadOnly] ref GhostRelative<TDescription> ghostRelative, ref Relative<TDescription> relative)
			{
				if (ghostRelative.GhostId == 0)
					return;

				if (GhostEntityMap.TryGetValue(ghostRelative.GhostId, out var ghostEntity))
				{
					relative.Target = ghostEntity.entity;
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new ConvertJob
			{
				GhostEntityMap = World.GetExistingSystem<GhostReceiveSystemGroup>().GhostEntityMap
			}.Schedule(this, inputDeps);
		}
	}
}