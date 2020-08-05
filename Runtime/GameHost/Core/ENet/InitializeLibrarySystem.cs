using System;
using GameHost.Transports.enet;
using Unity.Entities;

namespace Core.ENet
{
	public class InitializeLibrarySystem : SystemBase
	{
		protected override void OnCreate()
		{
			if (Library.Initialize())
				Console.WriteLine("[ENet] Initialize");
		}

		protected override void OnUpdate()
		{
		}

		protected override void OnDestroy()
		{
			if (Library.Initialized)
			{
				Library.Deinitialize();
				Console.WriteLine("[ENet] DeInitialized");
			}
		}
	}
}