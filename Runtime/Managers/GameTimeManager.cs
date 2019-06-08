using System;
using Unity.Burst;
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

            public void Execute(ref GameTimeComponent data)
            {
                data.Value.Time += DeltaTime;
                data.Value.Tick =  (int) (data.Value.Time * 1000f);

                data.Value.DeltaTime = DeltaTime;
                data.Value.DeltaTick = (int) (DeltaTime * 1000f);

                data.Value.Frame = ActualFrame;

                // For now we estimate it.
                data.Value.FixedTickPerSecond = data.Value.DeltaTick;
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

            inputDeps = new JobUpdateGameTime
            {
                DeltaTime   = dt,
                ActualFrame = fr
            }.Schedule(this, inputDeps);

            return inputDeps;
        }

        // Also create it in clients and servers
        /*[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
        public class ClientServerCreateSystem : ComponentSystem
        {
            protected override void OnCreate()
            {
                base.OnCreate();
                var gameTimeMgr = World.GetOrCreateSystem<GameTimeManager>();
                var serverGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
                var clientGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();

                serverGroup?.AddSystemToUpdateList(gameTimeMgr);
                clientGroup?.AddSystemToUpdateList(gameTimeMgr);

                gameTimeMgr.IsClient = clientGroup != null;
                gameTimeMgr.IsServer = serverGroup != null;
            }

            protected override void OnStartRunning()
            {
                var serverGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
                var clientGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();
                
                serverGroup?.RemoveSystemFromUpdateList(this);
                clientGroup?.RemoveSystemFromUpdateList(this);
                
                serverGroup?.SortSystemUpdateList();
                clientGroup?.SortSystemUpdateList();
            }

            protected override void OnUpdate()
            {
            }

            protected override void OnStopRunning()
            {
                base.OnStopRunning();
                
                var serverGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
                var clientGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();
                
                // now destroy this system
                if (serverGroup != null || clientGroup != null) 
                    World.DestroySystem(this);
            }
        }*/
    }
}