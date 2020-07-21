using System;
using System.Collections.Generic;
using GameHost.InputBackendFeature.Interfaces;
using package.stormiumteam.shared.ecs;
using Unity.Entities;

namespace GameHost.InputBackendFeature.Layouts
{
	public class RegisterInputActionSystem : SystemBase
	{
		internal Dictionary<string, Action<Entity>> ghActionToUnityLayoutMap;

		public RegisterInputActionSystem()
		{
			ghActionToUnityLayoutMap = new Dictionary<string, Action<Entity>>();
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			Enabled = false;
		}

		protected override void OnUpdate()
		{
		}

		public void Register<T>(string ghType)
			where T : struct, IInputAction
		{
			Console.WriteLine($"Register action type: {ghType}");
			ghActionToUnityLayoutMap[ghType] = e => EntityManager.SetOrAddComponentData(e, default(T));
		}

		public Entity TryGetCreateActionBase(string ghType)
		{
			if (ghActionToUnityLayoutMap.TryGetValue(ghType, out var create))
			{
				var ent = EntityManager.CreateEntity();
				create(ent);

				return ent;
			}

			return default;
		}
	}
}