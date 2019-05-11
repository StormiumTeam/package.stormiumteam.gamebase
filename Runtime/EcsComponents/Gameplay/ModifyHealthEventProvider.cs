using Unity.Entities;

namespace StormiumTeam.GameBase.Components
{
	[UpdateInGroup(typeof(HealthProcessGroup.BeforeGathering))] // be sure that the delayed entities are created before the gathering 
	public class ModifyHealthEventProvider : SystemProviderBatch<ModifyHealthEvent>
	{
		public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedStreamerComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(ModifyHealthEvent)
			};
			excludedStreamerComponents = null;
		}

		public override void SetEntityData(Entity entity, ModifyHealthEvent data)
		{
			EntityManager.SetComponentData(entity, data);
		}
	}
}