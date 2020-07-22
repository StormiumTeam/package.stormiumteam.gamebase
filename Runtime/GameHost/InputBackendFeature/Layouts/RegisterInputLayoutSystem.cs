using System;
using System.Collections.Generic;
using Unity.Entities;

namespace GameHost.InputBackendFeature.Layouts
{
	public class RegisterInputLayoutSystem : SystemBase
	{
		internal Dictionary<string, Type> ghLayoutToUnityLayoutMap;

		public RegisterInputLayoutSystem()
		{
			ghLayoutToUnityLayoutMap = new Dictionary<string, Type>();
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			Enabled = false;
		}

		protected override void OnUpdate()
		{
		}

		public void Register(string ghType, Type type)
		{
			if (!typeof(InputLayoutBase).IsAssignableFrom(type))
				return;

			Console.WriteLine($"Register layout type: {ghType}");
			ghLayoutToUnityLayoutMap[ghType] = type;
		}

		public void Register<T>(string ghType)
			where T : InputLayoutBase
		{
			Register(ghType, typeof(T));
		}

		public InputLayoutBase TryCreateLayout(string ghType, string layoutId)
		{
			if (ghLayoutToUnityLayoutMap.TryGetValue(ghType, out var type))
				return (InputLayoutBase) Activator.CreateInstance(type, layoutId);
			return null;
		}
	}
}