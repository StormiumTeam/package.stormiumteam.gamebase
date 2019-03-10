using System;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Stormium.Core
{
	public interface IGameLoop
	{
		World OnAwake();
		void  Shutdown();

		void OnUpdate();
		void OnFixedUpdate();
		void OnLateUpdate();
	}

	public class GameLoopManager : MonoBehaviour
	{
		/// <summary>
		/// This is the manager world. It shouldn't possess any data about the game.
		/// </summary>
		public static World ManagerWorld;

		private static Type      s_NextGameLoop;
		private static IGameLoop s_CurrentGameLoop;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		internal static void Init()
		{
			ManagerWorld = new World("Manager World");
			ManagerWorld.CreateManager<EntityManager>();

			var gameObject = new GameObject("GameLoopManager", typeof(GameLoopManager));
			
			DontDestroyOnLoad(gameObject);
			ForceSearchMetaData();
		}

		public static void RequestLoop<TGameLoop>()
			where TGameLoop : IGameLoop
		{
			Debug.Assert(s_NextGameLoop == null);
			
			Debug.Log("Requested GameLoop");

			s_NextGameLoop = typeof(TGameLoop);

			ForceLoadLoop();
		}

		public static void ForceSearchMetaData()
		{
			
		}

		private static void ShutdownLoop()
		{
			if (s_CurrentGameLoop == null) return;

			s_CurrentGameLoop.Shutdown();
			s_CurrentGameLoop = null;
		}

		private static void ForceLoadLoop()
		{
			try
			{
				ShutdownLoop();
				s_CurrentGameLoop = (IGameLoop) Activator.CreateInstance(s_NextGameLoop);

				World.Active = s_CurrentGameLoop.OnAwake();
			}
			finally
			{
				s_NextGameLoop = null;
			}
		}
		
		private void Update()
		{
			if (s_NextGameLoop != null)
			{
				ForceLoadLoop();
			}
		}
	}
}