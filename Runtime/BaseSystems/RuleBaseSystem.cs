using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;

namespace Runtime.BaseSystems
{
	public class RuleSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
		}
	}

	[UpdateInGroup(typeof(RuleSystemGroup))]
	public abstract class RuleSystemGroupBase : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
		}

		public virtual void Process()
		{
			base.OnUpdate();
		}
	}

	public class GameEventRuleSystemGroup : RuleSystemGroupBase
	{
	}

	public class PhysicsFilterRuleSystemGroup : RuleSystemGroupBase
	{
	}

	public abstract class RuleBaseSystem : GameBaseSystem
	{
		public virtual string Name        => "NoName";
		public virtual string Description => "NoDescription";
	}
}