using System;
using DefaultNamespace;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase.Components
{
	public struct RegenerativeHealthData : IComponentFromSnapshot<RegenerativeHealthData.SnapshotData>
	{
		[Serializable]
		public struct CreateInstance
		{
			public int    value, max;
			public float  rate;
			public int    cooldown;
			public Entity owner;
		}

		public struct SnapshotData : ISnapshotFromComponent<SnapshotData, RegenerativeHealthData>
		{
			public uint Tick { get; private set; }

			public int Value;
			public int Max;

			public int Rate;                // float * 1000
			public int CurrentRegeneration; // float * 1000

			public int  Cooldown;
			public uint StartTick;

			public void PredictDelta(uint tick, ref SnapshotData baseline1, ref SnapshotData baseline2)
			{
			}

			public void Serialize(ref SnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedInt(Value, compressionModel);
				writer.WritePackedInt(Max, compressionModel);

				writer.WritePackedInt(Rate, compressionModel);
				writer.WritePackedInt(CurrentRegeneration, compressionModel);

				writer.WritePackedInt(Cooldown, compressionModel);
				writer.WritePackedUInt(StartTick, compressionModel);
			}

			public void Deserialize(uint tick, ref SnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
			{
				Tick = tick;

				Value = reader.ReadPackedInt(ref ctx, compressionModel);
				Max   = reader.ReadPackedInt(ref ctx, compressionModel);

				Rate                = reader.ReadPackedInt(ref ctx, compressionModel);
				CurrentRegeneration = reader.ReadPackedInt(ref ctx, compressionModel);

				Cooldown  = reader.ReadPackedInt(ref ctx, compressionModel);
				StartTick = reader.ReadPackedUInt(ref ctx, compressionModel);
			}

			public void Interpolate(ref SnapshotData target, float factor)
			{
				Value               = (int) math.lerp(Value, target.Value, factor);
				Max                 = (int) math.lerp(Max, target.Max, factor);
				Rate                = (int) math.lerp(Rate, target.Rate, factor);
				CurrentRegeneration = (int) math.lerp(CurrentRegeneration, target.CurrentRegeneration, factor);
				Cooldown            = (int) math.lerp(Cooldown, target.Cooldown, factor);
				StartTick           = (uint) math.lerp(StartTick, target.StartTick, factor);
			}

			public void Set(RegenerativeHealthData component)
			{
				Value = component.Value;
				Max   = component.Max;

				Rate                = (int) (component.Rate * 1000);
				CurrentRegeneration = (int) (component.CurrentRegeneration * 1000);

				Cooldown  = component.Cooldown;
				StartTick = component.StartTick;
			}
		}

		public int Value;
		public int Max;

		public float Rate;
		public float CurrentRegeneration;

		public int  Cooldown;
		public uint StartTick;


		public void Set(SnapshotData snapshotData, NativeHashMap<int, GhostEntity> ghostMap)
		{
			Value = snapshotData.Value;
			Max   = snapshotData.Max;

			Rate                = snapshotData.Rate * 0.001f;
			CurrentRegeneration = snapshotData.CurrentRegeneration * 0.001f;

			Cooldown  = snapshotData.Cooldown;
			StartTick = snapshotData.StartTick;
		}

		public class UpdateFromSnapshot : BaseUpdateFromSnapshotSystem<SnapshotData, RegenerativeHealthData>
		{
		}

		[UpdateInGroup(typeof(HealthProcessGroup))]
		public class System : HealthProcessSystem
		{
			//[BurstCompile]
			private unsafe struct Job : IJobForEach<HealthContainerParent, RegenerativeHealthData, HealthConcreteValue>
			{
				public UTick Tick;
				public float Dt;

				[NativeDisableParallelForRestriction]
				public NativeList<ModifyHealthEvent> ModifyHealthEventList;

				public void Execute(ref HealthContainerParent  container,
				                    ref RegenerativeHealthData healthData,
				                    ref HealthConcreteValue    concrete)
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
							healthData.StartTick           = UTick.AddMs(Tick, (uint) healthData.Cooldown).Value;
						}
					}

					if (healthData.Cooldown <= 0 || Tick > healthData.StartTick)
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
					Tick                  = World.GetExistingSystem<ServerSimulationSystemGroup>().GetTick()
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
					ComponentType.ReadWrite<RegenerativeHealthData>(),
					ComponentType.ReadWrite<HealthConcreteValue>(),
					ComponentType.ReadWrite<HealthContainerParent>(),
					ComponentType.ReadWrite<DestroyChainReaction>()
				};
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