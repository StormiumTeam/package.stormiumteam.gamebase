using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StormiumTeam.GameBase.Systems
{
	public struct InputEvent
	{
		public InputAction.CallbackContext Context;

		public InputActionPhase Phase;
	}

	public abstract class BaseSyncInputSystem : GameBaseSystem
	{
		protected List<InputEvent> InputEvents = new List<InputEvent>();
		public    InputActionAsset Asset { get; private set; }

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
			InputEvents.Add(new InputEvent
			{
				Context = ctx,
				Phase   = ctx.phase
			});
		}

		protected void AddActionEvents(InputAction action)
		{
			action.started   += InputActionEvent;
			action.performed += InputActionEvent;
			action.canceled  += InputActionEvent;
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