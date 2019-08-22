using StormiumTeam.GameBase.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(PreConvertSystemGroup))]
	public class ConvertGhostEntityMap : JobComponentSystem
	{
		[BurstCompile]
		private struct Job : IJob
		{
			[NativeDisableContainerSafetyRestriction]
			public NativeHashMap<int, GhostEntity> GhostEntityMap;

			public NativeHashMap<int, Entity> TargetMap;

			public void Execute()
			{
				TargetMap.Clear();

				var keys   = GhostEntityMap.GetKeyArray(Allocator.Temp);
				var values = GhostEntityMap.GetValueArray(Allocator.Temp);

				for (var i = 0; i != keys.Length; i++)
				{
					if (!values[i].valid)
						continue;
					TargetMap.TryAdd(keys[i], values[i].entity);
				}
			}
		}

		public NativeHashMap<int, Entity> HashMap;
		public JobHandle                  dependency { get; private set; }

		private GhostReceiveSystemGroup m_ReceiveGroup;

		protected override void OnCreate()
		{
			HashMap = new NativeHashMap<int, Entity>(32, Allocator.Persistent);
		}

		protected override void OnStartRunning()
		{
			m_ReceiveGroup = World.GetExistingSystem<GhostReceiveSystemGroup>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return dependency = new Job
			{
				GhostEntityMap  = m_ReceiveGroup.GhostEntityMap,
				TargetMap       = HashMap
			}.Schedule(inputDeps);
		}

		protected override void OnDestroy()
		{
			HashMap.Dispose();
		}
	}
}