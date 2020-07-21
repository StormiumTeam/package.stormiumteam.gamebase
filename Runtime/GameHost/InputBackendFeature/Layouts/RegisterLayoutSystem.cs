﻿using System;
using System.Collections.Generic;
using Unity.Entities;

namespace GameHost.InputBackendFeature.Layouts
{
	public class RegisterLayoutSystem : SystemBase
	{
		internal Dictionary<string, Type> ghLayoutToUnityLayoutMap;

		protected override void OnCreate()
		{
			base.OnCreate();
			ghLayoutToUnityLayoutMap = new Dictionary<string, Type>();
			Enabled                  = false;
		}

		protected override void OnUpdate()
		{
		}

		public void Register(string ghType, Type type)
		{
			if (!typeof(InputLayoutBase).IsAssignableFrom(type))
				return;

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