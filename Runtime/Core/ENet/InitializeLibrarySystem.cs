using ENet;
using Unity.Entities;
using UnityEngine;

namespace Core.ENet
{
	public class InitializeLibrarySystem : SystemBase
	{
		protected override void OnCreate()
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