using System;
using System.Reflection;

namespace StormiumTeam.GameBase.Utility.Misc
{
	public static class TypeExt
	{
		private static string GetSub(MemberInfo type)
		{
			var sub = "";

			var next = type.DeclaringType;
			while (next != null)
			{
				sub  += next.Name + ".";
				next =  next.DeclaringType;
			}
			
			return sub;
		}
		
		public static string GetFriendlyName(Type type)
		{
			var friendlyName = type.Namespace + "::" + GetSub(type) + type.Name;
			if (!type.IsGenericType) 
				return friendlyName;
			
			var iBacktick = friendlyName.IndexOf('`');
			if (iBacktick > 0)
			{
				friendlyName = friendlyName.Remove(iBacktick);
			}
			friendlyName += "<";
			var typeParameters = type.GetGenericArguments();
			for (var i = 0; i < typeParameters.Length; ++i)
			{
				var typeParamName = GetFriendlyName(typeParameters[i]);
				friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
			}
			friendlyName += ">";

			return friendlyName;
		}
	}
}