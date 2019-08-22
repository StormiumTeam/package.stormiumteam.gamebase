using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace StormiumTeam.GameBase.Systems
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(GhostSpawnSystemGroup))]
	public class PreConvertSystemGroup : ComponentSystemGroup
	{	
	}
	
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(GhostSpawnSystemGroup))]
	public class PostConvertSystemGroup : ComponentSystemGroup
	{	
	}
	
	[UpdateInGroup(typeof(PostConvertSystemGroup))]
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
}