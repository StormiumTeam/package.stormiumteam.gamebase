using System;

namespace StormiumTeam.GameBase.Utility.Misc
{
	public static class TypeExt
	{
		public static string GetFriendlyName(Type type)
		{
			var friendlyName = type.Namespace + "::" + type.Name;
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