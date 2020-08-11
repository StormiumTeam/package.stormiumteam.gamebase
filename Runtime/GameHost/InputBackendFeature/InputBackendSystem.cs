using System;
using System.Collections.Generic;
using GameHost.Core.IO;
using GameHost.InputBackendFeature.Components;
using GameHost.InputBackendFeature.Layouts;
using package.stormiumteam.shared.ecs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.InputSystem;
using InputAction = GameHost.InputBackendFeature.Components.InputAction;

namespace GameHost.InputBackendFeature
{
	public class InputBackendSystem : SystemBase
	{
		private EntityQuery                                           actionQuery;
		private NativeHashMap<ReplicatedInputAction, Entity>          ghIdToEntityMap;
		private Dictionary<ReplicatedInputAction, InputActionLayouts> ghIdToLayoutsMap;

		private RegisterInputActionSystem inputActionSystem;

		private Dictionary<string, InputControl> cachedControlMap;
		
		protected override void OnCreate()
		{
			base.OnCreate();
			Enabled = false;

			actionQuery      = GetEntityQuery(typeof(ReplicatedInputAction));
			ghIdToEntityMap  = new NativeHashMap<ReplicatedInputAction, Entity>(256, Allocator.Persistent);
			ghIdToLayoutsMap = new Dictionary<ReplicatedInputAction, InputActionLayouts>(256);

			inputActionSystem = World.GetExistingSystem<RegisterInputActionSystem>();

			EntityManager.AddComponentData(EntityManager.CreateEntity(), new InputCurrentLayout {Id = null});
			
			cachedControlMap           =  new Dictionary<string, InputControl>();
			InputSystem.onDeviceChange += (device, change) =>
			{
				if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed)
					cachedControlMap.Clear();
			};
		}

		protected override void OnUpdate()
		{
		}

		protected override void OnDestroy()
		{
			ghIdToEntityMap.Dispose();

			foreach (var layout in ghIdToLayoutsMap.Values)
				layout.Clear();
			ghIdToLayoutsMap.Clear();
		}

		public void Register(uint id)
		{
		}

		public InputActionLayouts GetLayoutsOf(Entity entity)
		{
			if (!EntityManager.TryGetComponentData(entity, out ReplicatedInputAction inputAction))
				throw new InvalidOperationException($"GetLayoutsOf: {entity} should be an input action");

			return ghIdToLayoutsMap[EntityManager.GetComponentData<ReplicatedInputAction>(entity)];
		}

		public InputControl GetInputControl(string path)
		{
			if (!cachedControlMap.TryGetValue(path, out var control))
				cachedControlMap[path] = control = InputSystem.FindControl(path);

			return control;
		}

		internal void ClearCurrentActions()
		{
			EntityManager.DestroyEntity(actionQuery);
			ghIdToEntityMap.Clear();
		}

		internal Entity RegisterAction(TransportConnection connection, string ghActionType, InputAction action)
		{
			var entity = inputActionSystem.TryGetCreateActionBase(ghActionType);
			if (entity == default)
				return default;

			var repl = new ReplicatedInputAction
			{
				Connection = connection,
				Id         = action.Id
			};
			EntityManager.AddComponentData(entity, repl);

			ghIdToEntityMap[repl]  = entity;
			ghIdToLayoutsMap[repl] = new InputActionLayouts();
			return entity;
		}
	}
}