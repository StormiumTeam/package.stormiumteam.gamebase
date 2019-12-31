using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StormiumTeam.GameBase.Systems
{
	public struct InputEvent<TData>
		where TData : struct, IInitializeInputEventData
	{
		public InputAction.CallbackContext Context;
		public TData Data;

		public InputActionPhase Phase;
	}

	public interface IInitializeInputEventData
	{
		void Initialize(InputAction.CallbackContext ctx);
	}

	public abstract class BaseSyncInputSystem<TData> : GameBaseSystem
		where TData : struct, IInitializeInputEventData
	{
		protected List<InputEvent<TData>> InputEvents = new List<InputEvent<TData>>(4);
		public    InputActionAsset        Asset { get; private set; }

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
			var data = default(TData);
			data.Initialize(ctx);

			InputEvents.Add(new InputEvent<TData>
			{
				Context = ctx,
				Phase   = ctx.phase,
				Data    = data
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