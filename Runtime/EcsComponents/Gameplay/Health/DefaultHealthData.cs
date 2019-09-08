using System;
using Revolution;
using Revolution.NetCode;
using StormiumTeam.GameBase.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase.Components
{
	public struct DefaultHealthData : IComponentData
	{
		[Serializable]
		public struct CreateInstance
		{
			public int    value, max;
			public Entity owner;
		}

		public struct Exclude : IComponentData
		{
		}

		public int Value;
		public int Max;

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISnapshotDelta<Snapshot>, ISynchronizeImpl<DefaultHealthData>
		{
			public uint Tick { get; set; }

			public int Value;
			public int Max;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedInt(Value, compressionModel);
				writer.WritePackedInt(Max, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				Value = reader.ReadPackedInt(ref ctx, compressionModel);
				Max   = reader.ReadPackedInt(ref ctx, compressionModel);
			}

			public bool DidChange(Snapshot baseline)
			{
				return !(Value == baseline.Value
				         && Max == baseline.Max);
			}

			public void SynchronizeFrom(in DefaultHealthData component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				Value = component.Value;
				Max   = component.Max;
			}

			public void SynchronizeTo(ref DefaultHealthData component, in DeserializeClientData deserializeData)
			{
				component.Value = Value;
				component.Max   = Max;
			}
		}

		public class SynchronizeSnapshot : ComponentSnapshotSystem_Delta<DefaultHealthData, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class Update : ComponentUpdateSystem<DefaultHealthData, Snapshot>
		{
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