using System;
using DefaultNamespace;
using StormiumTeam.GameBase.Data;
using StormiumTeam.Networking.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace StormiumTeam.GameBase.Components
{
	public struct DefaultHealthData : IComponentFromSnapshot<DefaultHealthSnapshotData>
	{
		[Serializable]
		public struct CreateInstance
		{
			public int    value, max;
			public Entity owner;
		}

		public int Value;
		public int Max;

		public void Set(DefaultHealthSnapshotData snapshot, NativeHashMap<int, GhostEntity> ghostMap)
		{
			Value = snapshot.Value;
			Max   = snapshot.Max;

		}

		[UpdateInGroup(typeof(HealthProcessGroup))]
		public class System : HealthProcessSystem
		{
			[BurstCompile]
			private unsafe struct Job : IJobForEach<Owner, DefaultHealthData, HealthConcreteValue>
			{
				[NativeDisableParallelForRestriction]
				public NativeList<ModifyHealthEvent> ModifyHealthEventList;

				public void Execute(ref Owner               owner,
				                    ref DefaultHealthData   healthData,
				                    ref HealthConcreteValue concrete)
				{
					for (var i = 0; i != ModifyHealthEventList.Length; i++)
					{
						ref var ev = ref UnsafeUtilityEx.ArrayElementAsRef<ModifyHealthEvent>(ModifyHealthEventList.GetUnsafePtr(), i);
						if (ev.Target != owner.Target)
							continue;

						var difference = healthData.Value;
						switch (ev.Type)
						{
							case ModifyHealthType.Add:
								healthData.Value = math.clamp(healthData.Value + ev.Consumed, 0, healthData.Max);
								break;
							case ModifyHealthType.SetFixed:
								math.clamp(ev.Consumed, 0, healthData.Max);
								break;
							case ModifyHealthType.SetMax:
								healthData.Value = healthData.Max;
								break;
							case ModifyHealthType.SetNone:
								healthData.Value = 0;
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						ev.Consumed -= math.abs(healthData.Value - difference);
					}

					concrete.Value = healthData.Value;
					concrete.Max   = healthData.Max;
				}
			}

			protected override JobHandle Process(JobHandle jobHandle)
			{
				return new Job
				{
					ModifyHealthEventList = ModifyHealthEventList
				}.Schedule(this, jobHandle);
			}
		}

		public class InstanceProvider : BaseProviderBatch<CreateInstance>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new[]
				{
					ComponentType.ReadWrite<HealthDescription>(),
					ComponentType.ReadWrite<DefaultHealthData>(),
					ComponentType.ReadWrite<HealthConcreteValue>(),
					ComponentType.ReadWrite<Owner>(),
					ComponentType.ReadWrite<DestroyChainReaction>(),
					typeof(PlayEntityTag),
				};
			}

			public override void SetEntityData(Entity entity, CreateInstance data)
			{
				EntityManager.SetComponentData(entity, new DefaultHealthData {Value     = data.value, Max = data.max});
				EntityManager.SetComponentData(entity, new Owner {Target                = data.owner});
				EntityManager.SetComponentData(entity, new DestroyChainReaction {Target = data.owner});
			}
		}
	}
}