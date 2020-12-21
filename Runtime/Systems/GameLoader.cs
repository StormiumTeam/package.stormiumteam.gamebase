using System;
using System.Collections;
using BundleSystem;
using Cysharp.Threading.Tasks;
using Unity.Assertions;
using Unity.Entities;
using UnityEngine;
using UnityEngine.LowLevel;

namespace StormiumTeam.GameBase.Systems
{
	public static class GameLoader
	{
		public class Bootstrap : ICustomBootstrap
		{
			public bool Initialize(string defaultWorldName)
			{
				// Reset PlayerLoop
				var playerLoop = PlayerLoop.GetDefaultPlayerLoop();
				PlayerLoopHelper.Initialize(ref playerLoop);

				var world = new World(defaultWorldName);
				World.DefaultGameObjectInjectionWorld = world;

				var systemList = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default, false);
				DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systemList);
				
				ScriptBehaviourUpdateOrder.AddWorldToPlayerLoop(world, ref playerLoop);
				PlayerLoop.SetPlayerLoop(playerLoop);
				
				return true;
			}
		}
		
		private class __Start : MonoBehaviour
		{
			private BundleAsyncOperation operation;

			private void Awake()
			{
				BundleManager.LogMessages = true;

				operation = BundleManager.Initialize();
				StartCoroutine(CreateWorld());

				DontDestroyOnLoad(gameObject);
			}

			private IEnumerator CreateWorld()
			{
				while (operation.keepWaiting)
					yield return null;

				DefaultWorldInitialization.Initialize("Default World");
				if (!PlayerLoopHelper.IsInjectedUniTaskPlayerLoop())
					Debug.LogError("Not Injected");

				Loaded = true;
			}

			private void OnDestroy()
			{
				Loaded = false;
			}
		}

		public static bool Loaded { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Initialize()
		{
			Loaded = false;
			
			// ReSharper disable ObjectCreationAsStatement
			new GameObject("Start", typeof(__Start));
			// ReSharper restore ObjectCreationAsStatement
		}
	}
}