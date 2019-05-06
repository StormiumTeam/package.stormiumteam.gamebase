using System;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using NotImplementedException = System.NotImplementedException;

namespace StormiumTeam.GameBase.Components
{
	public struct DefaultHealthData : IComponentData, ISerializableAsPayload
	{
		[Serializable]
		public struct CreateInstance
		{
			public int    value, max;
			public Entity owner;
		}

		public int Value;
		public int Max;

		public void Write(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime runtime)
		{
			data.WriteDynamicIntWithMask((ulong) Value, (ulong) Max);
		}

		public void Read(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
		{
			data.ReadDynIntegerFromMask(out var unsignedValue, out var unsignedMax);

			Value = (int) unsignedValue;
			Max   = (int) unsignedMax;
		}

		public class Streamer : SnapshotEntityDataAutomaticStreamer<DefaultHealthData>
		{
		}

		[UpdateInGroup(typeof(HealthProcessGroup))]
		public class System : HealthProcessSystem
		{
			//[BurstCompile]
			private unsafe struct Job : IJobForEach<DefaultHealthData, HealthConcreteValue, OwnerState<LivableDescription>>
			{
				[NativeDisableParallelForRestriction]
				public NativeList<ModifyHealthEvent> ModifyHealthEventList;

				public void Execute(ref            DefaultHealthData              healthData,
				                    ref            HealthConcreteValue            concrete,
				                    [ReadOnly] ref OwnerState<LivableDescription> livableOwner)
				{
					for (var i = 0; i != ModifyHealthEventList.Length; i++)
					{
						ref var ev = ref UnsafeUtilityEx.ArrayElementAsRef<ModifyHealthEvent>(ModifyHealthEventList.GetUnsafePtr(), i);
						if (ev.Target != livableOwner.Target)
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
					concrete.Max = healthData.Max;
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

		public class InstanceProvider : SystemProviderBatch<CreateInstance>
		{
			public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedStreamerComponents)
			{
				entityComponents = new[]
				{
					ComponentType.ReadWrite<HealthDescription>(),
					ComponentType.ReadWrite<DefaultHealthData>(),
					ComponentType.ReadWrite<HealthConcreteValue>()
				};
				excludedStreamerComponents = null;
			}

			public override void SetEntityData(Entity entity, CreateInstance data)
			{
				EntityManager.SetComponentData(entity, new DefaultHealthData {Value = data.value, Max = data.max});
				if (data.owner != default)
				{
					EntityManager.ReplaceOwnerData(entity, data.owner);
					EntityManager.SetOrAddComponentData(entity, new DestroyChainReaction(data.owner));
				}
			}
		}
	}
}