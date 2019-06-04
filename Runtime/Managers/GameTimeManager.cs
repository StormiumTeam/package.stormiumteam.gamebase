using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using static Unity.Mathematics.math;

namespace StormiumTeam.GameBase
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class GameTimeManager : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(EntityAuthority))]
        private struct JobUpdateGameTime : IJobForEach<GameTimeComponent>
        {
            public int ActualTick;
            public int ActualFrame;

            public void Execute(ref GameTimeComponent data)
            {
                var tps = clamp(ActualTick - data.Value.Tick, 1, 300);

                data.Value.Tick      = ActualTick;
                data.Value.Time      = ActualTick * 0.001f;
                data.Value.Frame     = ActualFrame;
                data.Value.DeltaTick = tps;
                data.Value.DeltaTime = tps * 0.001f;

                // For now we estimate it.
                data.Value.FixedTickPerSecond = tps;
            }
        }
        
        [BurstCompile]
        private struct JobUpdateSingleton : IJobForEach<SingletonGameTime>
        {
            public int ActualTick;
            public int ActualFrame;

            public void Execute(ref SingletonGameTime data)
            {
                var tps = clamp(ActualTick - data.Tick, 1, 300);

                data.Tick      = ActualTick;
                data.Time      = ActualTick * 0.001f;
                data.Frame     = ActualFrame;
                data.DeltaTick = tps;
                data.DeltaTime = tps * 0.001f;

                // For now we estimate it.
                data.FixedTickPerSecond = tps;
            }
        }

        protected override void OnCreate()
        {
            EntityManager.CreateEntity(typeof(SingletonGameTime));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new JobUpdateSingleton
            {
                ActualTick  = (int)(Time.unscaledTime * 1000),
                ActualFrame = Time.frameCount
            }.Schedule(this, inputDeps);
            inputDeps = new JobUpdateGameTime
            {
                ActualTick  = (int)(Time.unscaledTime * 1000),
                ActualFrame = Time.frameCount
            }.Schedule(this, inputDeps);

            return inputDeps;
        }
    }
}