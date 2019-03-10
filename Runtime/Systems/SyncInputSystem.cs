using System;
using System.Collections.Generic;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using Stormium.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Input;

namespace Runtime.Systems
{
	public abstract class SyncInputSystem : GameBaseSyncMessageSystem
	{
		public InputActionAsset Asset { get; private set; }

		protected bool Refresh(InputActionAsset asset)
		{
			Asset = asset;
			Asset.Enable();
			
			OnAssetRefresh();

			return Asset != null;
		}

		protected abstract void OnAssetRefresh();
	}
}