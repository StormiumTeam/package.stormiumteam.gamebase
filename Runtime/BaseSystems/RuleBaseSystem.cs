using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;

namespace Runtime.BaseSystems
{
	public class RuleSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
			// Rules are not automatically updated here.
		}

		public void Process()
		{
			foreach (var componentSystem in m_systemsToUpdate)
			{
				var ruleSystem = (RuleBaseSystem) componentSystem;
			}
		}
	}
	
	[UpdateInGroup(typeof(RuleSystemGroup))]
	public abstract class RuleBaseSystem : GameBaseSystem
	{
	}
}