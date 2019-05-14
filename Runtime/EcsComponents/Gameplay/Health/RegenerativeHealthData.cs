using System;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using StormiumShared.Core;
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
	public struct RegenerativeHealthData : IComponentData, ISerializableAsPayload
	{
		[Serializable]
		public struct CreateInstance
		{
			public int    value, max;
			public float    rate;
			public int cooldown;
			public Entity owner;
		}

		public int Value;
		public int Max;

		public float Rate;
		public float CurrentRegeneration;
		
		public int Cooldown;
		public int StartTick;

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

		public class Streamer : SnapshotEntityDataAutomaticStreamer<RegenerativeHealthData>
		{
		}

		[UpdateInGroup(typeof(HealthProcessGroup))]
		public class System : HealthProcessSystem
		{
			//[BurstCompile]
			private unsafe struct Job : IJobForEach<HealthContainerParent, RegenerativeHealthData, HealthConcreteValue>
			{
				public int Tick;
				public float Dt;
				
				[NativeDisableParallelForRestriction]
				public NativeList<ModifyHealthEvent> ModifyHealthEventList;

				public void Execute(ref HealthContainerParent container,
				                    ref RegenerativeHealthData healthData,
				                    ref HealthConcreteValue   concrete)
				{
					for (var i = 0; i != ModifyHealthEventList.Length; i++)
					{
						ref var ev = ref UnsafeUtilityEx.ArrayElementAsRef<ModifyHealthEvent>(ModifyHealthEventList.GetUnsafePtr(), i);
						if (ev.Target != container.Parent)
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

						if (healthData.Value < difference)
						{
							healthData.CurrentRegeneration = default;
							healthData.StartTick = Tick + healthData.Cooldown;
						}
					}

					if (healthData.Cooldown <= 0 || healthData.StartTick <= Tick)
						healthData.CurrentRegeneration += healthData.Rate * Dt;

					if (healthData.CurrentRegeneration >= 1.0f)
					{
						var asInt = (int) healthData.CurrentRegeneration;

						healthData.Value += asInt;
						healthData.CurrentRegeneration -= asInt;
					}

					if (healthData.Value > healthData.Max) healthData.Value = healthData.Max;

					concrete.Value = healthData.Value;
					concrete.Max   = healthData.Max;
				}
			}

			protected override JobHandle Process(JobHandle jobHandle)
			{
				return new Job
				{
					ModifyHealthEventList = ModifyHealthEventList,
					Tick = GetSingleton<SingletonGameTime>().Tick,
					Dt = GetSingleton<SingletonGameTime>().DeltaTime
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
					ComponentType.ReadWrite<RegenerativeHealthData>(),
					ComponentType.ReadWrite<HealthConcreteValue>(),
					ComponentType.ReadWrite<HealthContainerParent>(),
					ComponentType.ReadWrite<DestroyChainReaction>()
				};
				excludedStreamerComponents = null;
			}

			public override void SetEntityData(Entity entity, CreateInstance data)
			{
				EntityManager.SetComponentData(entity, new RegenerativeHealthData {Value = data.value, Max = data.max, Rate = data.rate, Cooldown = data.cooldown});
				EntityManager.SetComponentData(entity, new HealthContainerParent(data.owner));
				EntityManager.SetComponentData(entity, new DestroyChainReaction {Target = data.owner});
			}
		}
	}
}