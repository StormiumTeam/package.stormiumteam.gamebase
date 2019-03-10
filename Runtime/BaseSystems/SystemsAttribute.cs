using System;

namespace Stormium.Core
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