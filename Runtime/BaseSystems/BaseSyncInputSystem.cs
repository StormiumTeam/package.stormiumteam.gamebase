using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace StormiumTeam.GameBase.Systems
{
	public abstract class BaseSyncInputSystem : GameBaseSystem
	{
		protected List<InputAction.CallbackContext> InputEvents = new List<InputAction.CallbackContext>();
		public    InputActionAsset                  Asset { get; private set; }

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
			foreach (var action in map.actions)
			{
				action.performed -= InputActionEvent;
				action.started   -= InputActionEvent;
				action.canceled  -= InputActionEvent;
			}

			Asset.Disable();
		}

		protected abstract void OnAssetRefresh();

		protected virtual void InputActionEvent(InputAction.CallbackContext ctx)
		{
			InputEvents.Add(ctx);
		}

		protected void AddActionEvents(InputAction action, Action<InputAction.CallbackContext> customCallback = null)
		{
			var c = customCallback ?? InputActionEvent;
			action.started   += c;
			action.performed += c;
			action.canceled  += c;
		}
	}

	public abstract class JobSyncInputSystem : JobGameBaseSystem
	{
		protected List<InputAction.CallbackContext> InputEvents = new List<InputAction.CallbackContext>();
		public    InputActionAsset                  Asset { get; private set; }

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
			foreach (var action in map.actions)
			{
				action.performed -= InputActionEvent;
				action.started   -= InputActionEvent;
				action.canceled  -= InputActionEvent;
			}

			Asset.Disable();
		}

		protected abstract void OnAssetRefresh();

		protected virtual void InputActionEvent(InputAction.CallbackContext ctx)
		{
			InputEvents.Add(ctx);
		}

		protected void AddActionEvents(InputAction action)
		{
			action.started   += InputActionEvent;
			action.performed += InputActionEvent;
			action.canceled  += InputActionEvent;
		}
	}
}