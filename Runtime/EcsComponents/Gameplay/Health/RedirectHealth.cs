using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace StormiumTeam.GameBase.Components
{
	public struct RedirectHealth : IComponentData
	{
		public Entity Target;

		[UpdateInGroup(typeof(HealthProcessGroup))]
		[UpdateBefore(typeof(DefaultHealthData.System))]
		// todo: we should make a group for pre-health systems
		public class System : HealthProcessSystem
		{
			private struct Job : IJobForEachWithEntity<RedirectHealth>
			{
				[NativeDisableParallelForRestriction]
				public NativeList<ModifyHealthEvent> ModifyHealthEventList;

				public void Execute(Entity             entity,
				                    int                _,
				                    ref RedirectHealth redirectData)
				{
					for (var i = 0; i != ModifyHealthEventList.Length; i++)
					{
						var ev = ModifyHealthEventList[i];
						if (ev.Target != entity)
							continue;

						Debug.Log($"Redirecting health event from {entity} to {redirectData.Target}.");

						ev.Target = entity;
						ModifyHealthEventList.Add(ev);
					}
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