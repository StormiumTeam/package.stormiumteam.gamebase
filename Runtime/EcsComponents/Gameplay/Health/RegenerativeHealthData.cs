using System;
using Revolution;
using Revolution.NetCode;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase.Components
{
	public struct RegenerativeHealthData : IComponentData
	{
		[Serializable]
		public struct CreateInstance
		{
			public int    value, max;
			public float  rate;
			public int    cooldown;
			public Entity owner;
		}

		public struct Exclude : IComponentData
		{
		}

		public int Value;
		public int Max;

		public float Rate;
		public float CurrentRegeneration;

		public int  Cooldown;
		public uint StartTick;

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISnapshotDelta<Snapshot>, ISynchronizeImpl<RegenerativeHealthData>
		{
			public uint Tick { get; set; }

			public int Value;
			public int Max;

			public int Rate;                // float * 1000
			public int CurrentRegeneration; // float * 1000

			public int  Cooldown;
			public uint StartTick;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedInt(Value, compressionModel);
				writer.WritePackedInt(Max, compressionModel);

				writer.WritePackedInt(Rate, compressionModel);
				writer.WritePackedInt(CurrentRegeneration, compressionModel);

				writer.WritePackedInt(Cooldown, compressionModel);
				writer.WritePackedUInt(StartTick, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				Value = reader.ReadPackedInt(ref ctx, compressionModel);
				Max   = reader.ReadPackedInt(ref ctx, compressionModel);

				Rate                = reader.ReadPackedInt(ref ctx, compressionModel);
				CurrentRegeneration = reader.ReadPackedInt(ref ctx, compressionModel);

				Cooldown  = reader.ReadPackedInt(ref ctx, compressionModel);
				StartTick = reader.ReadPackedUInt(ref ctx, compressionModel);
			}

			public bool DidChange(Snapshot baseline)
			{
				return true;
			}

			public void SynchronizeFrom(in RegenerativeHealthData component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				Value = component.Value;
				Max   = component.Max;

				Rate                = (int) (component.Rate * 1000);
				CurrentRegeneration = (int) (component.CurrentRegeneration * 1000);

				Cooldown  = component.Cooldown;
				StartTick = component.StartTick;
			}

			public void SynchronizeTo(ref RegenerativeHealthData component, in DeserializeClientData deserializeData)
			{
				component.Value = Value;
				component.Max   = Max;

				component.Rate                = Rate * 0.001f;
				component.CurrentRegeneration = CurrentRegeneration * 0.001f;

				component.Cooldown  = Cooldown;
				component.StartTick = StartTick;
			}
		}

		public class SynchronizeSnapshot : ComponentSnapshotSystem_Delta<RegenerativeHealthData, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class Update : ComponentUpdateSystemDirect<RegenerativeHealthData, Snapshot>
		{
		}

		[UpdateInGroup(typeof(HealthProcessGroup))]
		public class System : HealthProcessSystem
		{
			//[BurstCompile]
			private unsafe struct Job : IJobForEach<Owner, RegenerativeHealthData, HealthConcreteValue>
			{
				public UTick Tick;
				public float Dt;

				[NativeDisableParallelForRestriction]
				public NativeList<ModifyHealthEvent> ModifyHealthEventList;

				public void Execute(ref Owner                  owner,
				                    ref RegenerativeHealthData healthData,
				                    ref HealthConcreteValue    concrete)
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

						if (healthData.Value < difference)
						{
							healthData.CurrentRegeneration = default;
							healthData.StartTick           = UTick.AddMs(Tick, (uint) healthData.Cooldown).AsUInt;
						}
					}

					Dt = Tick.Delta;
					if (healthData.Cooldown <= 0 || Tick.AsUInt > healthData.StartTick)
						healthData.CurrentRegeneration += healthData.Rate * Dt;

					if (healthData.CurrentRegeneration >= 1.0f)
					{
						var asInt = (int) healthData.CurrentRegeneration;

						healthData.Value               += asInt;
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
					Tick                  = ServerTick
				}.Schedule(this, jobHandle);
			}
		}

		[UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities))]
		public class InstanceProvider : BaseProviderBatch<CreateInstance>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new[]
				{
					ComponentType.ReadWrite<HealthDescription>(),
					ComponentType.ReadWrite<RegenerativeHealthData>(),
					ComponentType.ReadWrite<HealthConcreteValue>(),
					ComponentType.ReadWrite<Owner>(),
					ComponentType.ReadWrite<DestroyChainReaction>(),
					typeof(PlayEntityTag),
				};
			}

			public override void SetEntityData(Entity entity, CreateInstance data)
			{
				EntityManager.SetComponentData(entity, new RegenerativeHealthData {Value = data.value, Max = data.max, Rate = data.rate, Cooldown = data.cooldown});
				EntityManager.SetComponentData(entity, new Owner {Target                 = data.owner});
				EntityManager.SetComponentData(entity, new DestroyChainReaction {Target  = data.owner});
			}
		}
	}
}