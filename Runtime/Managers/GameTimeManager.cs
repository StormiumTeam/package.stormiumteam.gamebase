using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;
using static Unity.Mathematics.math;

namespace StormiumTeam.GameBase
{
    [UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
    public class GameTimeManager : JobComponentSystem
    {
        [BurstCompile]
        private struct JobUpdateGameTime : IJobForEach<GameTimeComponent>
        {
            public float DeltaTime;
            public int   ActualFrame;

            public NativeArray<GameTimeComponent> LastGameTimeComponent;

            public void Execute(ref GameTimeComponent data)
            {
                data.Value.Time += DeltaTime;
                data.Value.Tick =  (int) (data.Value.Time * 1000f);

                data.Value.DeltaTime = DeltaTime;
                data.Value.DeltaTick = (int) (DeltaTime * 1000f);

                data.Value.Frame = ActualFrame;

                // For now we estimate it.
                data.Value.FixedTickPerSecond = data.Value.DeltaTick;

                LastGameTimeComponent[0] = data;
            }
        }

        [BurstCompile]
        private struct UpdateSynchronizedTimeJob : IJobForEach<SynchronizedSimulationTime>
        {
            public NativeArray<GameTimeComponent> LastGameTimeComponent;

            public void Execute(ref SynchronizedSimulationTime synchronizedSimulationTime)
            {
                synchronizedSimulationTime.Interpolated = (uint) LastGameTimeComponent[0].Tick;
                synchronizedSimulationTime.Predicted    = (uint) LastGameTimeComponent[0].Tick;
            }
        }

        private struct DisposeJob : IJob
        {
            [DeallocateOnJobCompletion]
            public NativeArray<GameTimeComponent> LastGameTimeComponent;

            public void Execute()
            {
            }
        }

        internal bool IsClient, IsServer;

        protected override void OnCreate()
        {
            EntityManager.CreateEntity(typeof(GameTimeComponent));

            var serverGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
            var clientGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();

            IsServer = serverGroup != null;
            IsClient = clientGroup != null;

            if (IsServer)
            {
                EntityManager.CreateEntity(typeof(SynchronizedSimulationTime), typeof(GhostComponent));
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var dt = Time.deltaTime;
            var fr = Time.frameCount;

            if (IsServer)
            {
                var topGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
                dt = topGroup.UpdateDeltaTime;
                fr = (int) topGroup.ServerTick;
            }

            if (IsClient)
            {
                var topGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();
                dt = topGroup.UpdateDeltaTime;
                fr = (int) NetworkTimeSystem.TimestampMS;
            }

            var lastGameTimeComponent = new NativeArray<GameTimeComponent>(1, Allocator.TempJob);

            inputDeps = new JobUpdateGameTime
            {
                LastGameTimeComponent = lastGameTimeComponent,

                DeltaTime   = dt,
                ActualFrame = fr
            }.Schedule(this, inputDeps);

            if (IsServer)
            {
                inputDeps = new UpdateSynchronizedTimeJob
                {
                    LastGameTimeComponent = lastGameTimeComponent
                }.Schedule(this, inputDeps);
                inputDeps = World.GetExistingSystem<SynchronizedSimulationTimeSystem>().CustomUpdate(inputDeps);
            }

            inputDeps = new DisposeJob
            {
                LastGameTimeComponent = lastGameTimeComponent
            }.Schedule(inputDeps);

            return inputDeps;
        }
    }
}