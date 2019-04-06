using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace StormiumTeam.GameBase.Components
{
	public struct DefaultHealthData : IComponentData
	{
		public int Value;
		public int Max;

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

						healthData.Value = math.select
						(
							math.clamp(healthData.Value + ev.Consumed, 0, healthData.Max),
							math.clamp(ev.Consumed, 0, healthData.Max),
							ev.SetFixedHealth
						);

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
	}
}