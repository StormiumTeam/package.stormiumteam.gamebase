using System;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
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
			[BurstCompile]
			private unsafe struct Job : IJobProcessComponentData<DefaultHealthData, HealthConcreteValue, OwnerState<LivableDescription>>
			{
				public NativeList<ModifyHealthEvent> ModifyHealthEventList;

				public void Execute(ref            DefaultHealthData              healthData,
				                    ref            HealthConcreteValue            concrete,
				                    [ReadOnly] ref OwnerState<LivableDescription> livableOwner)
				{
					for (var i = 0; i != ModifyHealthEventList.Length; i++)
					{
						ref var ev = ref UnsafeUtilityEx.ArrayElementAsRef<ModifyHealthEvent>(ModifyHealthEventList.GetUnsafePtr(), i);
						if (ev.Target != livableOwner.Target)
							return;

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

					// fast copy/paste
					concrete = *(HealthConcreteValue*) UnsafeUtility.AddressOf(ref healthData);
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

		public class InstanceProvider : SystemProvider<CreateInstance>
		{
			public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedStreamerComponents)
			{
				entityComponents = new[]
				{
					ComponentType.ReadWrite<HealthDescription>(),
					ComponentType.ReadWrite<DefaultHealthData>()
				};
				excludedStreamerComponents = null;
			}

			public override Entity SpawnLocalEntityWithArguments(CreateInstance data)
			{
				var local = SpawnLocal();
				EntityManager.SetComponentData(local, new DefaultHealthData {Value = data.value, Max = data.max});
				if (data.owner != default)
				{
					EntityManager.ReplaceOwnerData(local, data.owner);
					EntityManager.AddComponentData(local, new DestroyChainReaction(data.owner));

				}

				return local;
			}

			public Entity SpawnLocal(int value, int max, Entity owner)
			{
				return SpawnLocalEntityWithArguments(new CreateInstance {value = value, max = max});
			}
		}
	}
}