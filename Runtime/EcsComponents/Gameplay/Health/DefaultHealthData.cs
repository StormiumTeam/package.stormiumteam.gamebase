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
	public struct DefaultHealthData : IComponentFromSnapshot<DefaultHealthData.SnapshotData>
	{
		[Serializable]
		public struct CreateInstance
		{
			public int    value, max;
			public Entity owner;
		}

		public struct SnapshotData : ISnapshotFromComponent<SnapshotData, DefaultHealthData>
		{
			public uint Tick { get; private set; }

			public int OwnerGhostId;
			
			public int Value;
			public int Max;

			public void PredictDelta(uint tick, ref SnapshotData baseline1, ref SnapshotData baseline2)
			{
			}

			public void Interpolate(ref SnapshotData target, float factor)
			{
				Value = (int) math.lerp(Value, target.Value, factor);
				Max   = (int) math.lerp(Max, target.Max, factor);
			}
			
			public void Serialize(ref SnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedInt(OwnerGhostId, compressionModel);
				writer.WritePackedInt(Value, compressionModel);
				writer.WritePackedInt(Max, compressionModel);
			}

			public void Deserialize(uint tick, ref SnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
			{
				Tick = tick;

				OwnerGhostId = reader.ReadPackedInt(ref ctx, compressionModel);
				Value = reader.ReadPackedInt(ref ctx, compressionModel);
				Max   = reader.ReadPackedInt(ref ctx, compressionModel);
			}
			
			public void Set(DefaultHealthData component)
			{
				Value = component.Value;
				Max   = component.Max;
			}
		}

		public int Value;
		public int Max;

		public void Set(SnapshotData snapshot, NativeHashMap<int, GhostEntity> ghostMap)
		{
			Value = snapshot.Value;
			Max   = snapshot.Max;
		}

		public class SynchronizeFromSnapshot : BaseUpdateFromSnapshotSystem<SnapshotData, DefaultHealthData>
		{
		}

		[UpdateInGroup(typeof(HealthProcessGroup))]
		public class System : HealthProcessSystem
		{
			//[BurstCompile]
			private unsafe struct Job : IJobForEach<HealthContainerParent, DefaultHealthData, HealthConcreteValue>
			{
				[NativeDisableParallelForRestriction]
				public NativeList<ModifyHealthEvent> ModifyHealthEventList;

				public void Execute(ref HealthContainerParent container,
				                    ref DefaultHealthData     healthData,
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
					ComponentType.ReadWrite<HealthContainerParent>(),
					ComponentType.ReadWrite<DestroyChainReaction>()
				};
			}

			public override void SetEntityData(Entity entity, CreateInstance data)
			{
				EntityManager.SetComponentData(entity, new DefaultHealthData {Value = data.value, Max = data.max});
				EntityManager.SetComponentData(entity, new HealthContainerParent(data.owner));
				EntityManager.SetComponentData(entity, new DestroyChainReaction {Target = data.owner});
			}
		}
	}
}