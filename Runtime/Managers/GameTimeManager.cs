using System;
using StormiumShared.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using static Unity.Mathematics.math;

namespace StormiumTeam.GameBase
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class GameTimeManager : JobComponentSystem
    {
        [BurstCompile]
        private struct Job : IJobProcessComponentData<GameTimeComponent>
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

        protected override void OnCreateManager()
        {
            EntityManager.CreateEntity(typeof(GameTimeComponent));
            
            SetSingleton(default(GameTimeComponent));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                ActualTick  = (int)(Time.unscaledTime * 1000),
                ActualFrame = Time.frameCount
            }.Schedule(this, inputDeps);
        }
    }
}