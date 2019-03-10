using System;

namespace StormiumTeam.GameBase
{
	public class PersistentSystemAttribute : Attribute
	{
		
	}

	public class InstanceSystemAttribute : Attribute
	{
		public Type[] GameLoopRestriction;

		public InstanceSystemAttribute(params Type[] restrictions)
		{
			GameLoopRestriction = restrictions;
		}
	}
}