using GameHost.Transports.enet;
using Unity.Entities;
using UnityEngine;

namespace Core.ENet
{
	public class InitializeLibrarySystem : SystemBase
	{
		public InitializeLibrarySystem()
		{
			if (Library.Initialize())
				Debug.Log("ENet Initialized");
		}

		protected override void OnUpdate()
		{
		}

		protected override void OnDestroy()
		{
			Library.Deinitialize();
		}
	}
}