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

        private ServerSimulationSystemGroup m_ServerGroup;
        private ClientSimulationSystemGroup m_ClientGroup;

        protected override void OnCreate()
        {
            EntityManager.CreateEntity(typeof(GameTimeComponent));

            m_ServerGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
            m_ClientGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();

            IsServer = m_ServerGroup != null;
            IsClient = m_ClientGroup != null;

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
                dt = m_ServerGroup.UpdateDeltaTime;
                fr = (int) m_ServerGroup.ServerTick;
            }

            if (IsClient)
            {
                dt = m_ClientGroup.UpdateDeltaTime;
                fr = (int) NetworkTimeSystem.TimestampMS;
            }

            var lastGameTimeComponent = new NativeArray<GameTimeComponent>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

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
                var system = World.GetExistingSystem<SynchronizedSimulationTimeSystem>();
                system.InputDependency = inputDeps;
                system.Enabled = true;
                system.Update();
                system.Enabled = false;
                inputDeps = JobHandle.CombineDependencies(inputDeps, system.OutputDependency);
            }

            inputDeps = new DisposeJob
            {
                LastGameTimeComponent = lastGameTimeComponent
            }.Schedule(inputDeps);

            return inputDeps;
        }
    }
}