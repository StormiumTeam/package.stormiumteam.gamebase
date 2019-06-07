using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StormiumTeam.GameBase.Systems
{
	public abstract class BaseSyncInputSystem : GameBaseSystem
	{
		public InputActionAsset Asset { get; private set; }

		protected bool Refresh(InputActionAsset asset)
		{
			Asset = asset;
			Asset.Enable();
			
			OnAssetRefresh();

			return Asset != null;
		}
		
		protected override void OnDestroy()
		{
			base.OnDestroy();
			
			Asset.Disable();
		}

		protected abstract void OnAssetRefresh();
	}
	
	public abstract class JobSyncInputSystem : JobGameBaseSystem
	{
		public InputActionAsset Asset { get; private set; }

		protected List<InputAction.CallbackContext> InputEvents = new List<InputAction.CallbackContext>();
		
		protected bool Refresh(InputActionAsset asset)
		{
			Asset = asset;
			Asset.Enable();
			
			OnAssetRefresh();

			return Asset != null;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (Asset == null)
				return;
			
			foreach (var map in Asset.actionMaps)
			{
				foreach (var action in map.actions)
				{
					action.performed -= InputActionEvent;
					action.started -= InputActionEvent;
					action.canceled -= InputActionEvent;
				}
			}
			Asset.Disable();
		}

		protected abstract void OnAssetRefresh();

		protected virtual void InputActionEvent(InputAction.CallbackContext ctx)
		{
			InputEvents.Add(ctx);
		}
	}
}