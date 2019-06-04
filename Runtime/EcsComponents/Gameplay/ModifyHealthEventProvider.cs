using Unity.Entities;

namespace StormiumTeam.GameBase.Components
{
	[UpdateInGroup(typeof(HealthProcessGroup.BeforeGathering))] // be sure that the delayed entities are created before the gathering 
	public class ModifyHealthEventProvider : BaseProviderBatch<ModifyHealthEvent>
	{
		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(ModifyHealthEvent)
			};
		}

		public override void SetEntityData(Entity entity, ModifyHealthEvent data)
		{
			EntityManager.SetComponentData(entity, data);
		}
	}
}