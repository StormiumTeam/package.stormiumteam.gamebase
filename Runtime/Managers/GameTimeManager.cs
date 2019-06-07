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

        protected override void OnCreate()
        {
            EntityManager.CreateEntity(typeof(GameTimeComponent));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new JobUpdateGameTime
            {
                DeltaTime   = Time.deltaTime,
                ActualFrame = Time.frameCount
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
                var serverGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
                var clientGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();

                serverGroup?.AddSystemToUpdateList(World.GetOrCreateSystem<GameTimeManager>());
                clientGroup?.AddSystemToUpdateList(World.GetOrCreateSystem<GameTimeManager>());
                
                Debug.Log(World.Name);
                
                // now destroy this system
                if (serverGroup != null || clientGroup != null) 
                    World.DestroySystem(this);
                serverGroup?.RemoveSystemFromUpdateList(this);
                clientGroup?.RemoveSystemFromUpdateList(this);
                
                serverGroup?.SortSystemUpdateList();
                clientGroup?.SortSystemUpdateList();
            }

            protected override void OnUpdate()
            {
            }
        }*/
    }
}