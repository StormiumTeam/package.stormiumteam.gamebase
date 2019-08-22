using GmMachine;
using Misc.GmMachine.Contexts;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.Data;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;

namespace StormiumTeam.GameBase
{
	public struct ExecutingGameMode : IComponentData
	{
		public int            TypeIndex;
		public NativeString64 Name;
	}

	public struct GameModeOnFirstInit : IComponentData
	{
	}

	public interface IGameMode : ISystemStateComponentData
	{
	}

	[DisableAutoCreation]
	public class GameModeManager : ComponentSystem
	{
		private EntityQuery m_RunningGameMode;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_RunningGameMode = GetEntityQuery(typeof(ExecutingGameMode));
		}

		protected override void OnUpdate()
		{

		}

		public void SetGameMode<T>(T data, string name = null, bool serialize = true)
			where T : struct, IGameMode
		{
			EntityManager.DestroyEntity(m_RunningGameMode);

			var entity = EntityManager.CreateEntity(typeof(ExecutingGameMode), typeof(T), typeof(GameModeOnFirstInit));
			EntityManager.SetComponentData(entity, new ExecutingGameMode
			{
				TypeIndex = ComponentType.ReadWrite<T>().TypeIndex,
				Name      = new NativeString64(name ?? typeof(T).Name)
			});
			EntityManager.SetComponentData(entity, data);

#if UNITY_EDITOR
			EntityManager.SetName(entity, $"GameMode: {name ?? typeof(T).Name}");
#endif
			EntityManager.AddComponent(entity, typeof(GhostComponent));
		}

		public void SetGameMode<T>(Entity entity, string name = null, bool serialize = true)
			where T : struct, IGameMode
		{
			EntityManager.DestroyEntity(m_RunningGameMode);

			EntityManager.SetOrAddComponentData(entity, new GameModeOnFirstInit());
			EntityManager.SetOrAddComponentData(entity, new ExecutingGameMode
			{
				TypeIndex = ComponentType.ReadWrite<T>().TypeIndex,
				Name      = new NativeString64(name ?? typeof(T).Name)
			});
			EntityManager.AddComponent(entity, typeof(GhostComponent));
		}
	}

	[UpdateInGroup(typeof(GameModeSystemGroup))]
	public abstract class GameModeSystem<TGameMode> : GameBaseSystem
		where TGameMode : struct, IGameMode
	{
		private EntityQuery m_GameModeQuery;
		private EntityQuery m_ExecutingMapQuery;
		private EntityQuery m_MapLoadQuery;

		private MapManager m_MapManager;

		private Entity m_LoopEntity;

		protected MapManager MapManager => m_MapManager;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_MapManager = World.GetOrCreateSystem<MapManager>();

			m_GameModeQuery     = GetEntityQuery(typeof(ExecutingGameMode), typeof(TGameMode));
			m_ExecutingMapQuery = GetEntityQuery(typeof(ExecutingMapData));
			m_MapLoadQuery      = GetEntityQuery(typeof(OperationMapLoadTag));
		}

		public abstract void OnGameModeUpdate(Entity entity, ref TGameMode gameMode);

		protected override void OnUpdate()
		{
			Entities.ForEach((Entity entity, ref TGameMode gameMode) =>
			{
				m_LoopEntity = entity;
				OnGameModeUpdate(entity, ref gameMode);
			});
		}

		public bool IsInitialization()
		{
			return EntityManager.HasComponent(m_LoopEntity, typeof(GameModeOnFirstInit));
		}

		public void FinishInitialization()
		{
			EntityManager.RemoveComponent(m_LoopEntity, typeof(GameModeOnFirstInit));
		}

		public bool IsCleanUp()
		{
			return !EntityManager.HasComponent(m_LoopEntity, typeof(ExecutingGameMode));
		}

		public void FinishCleanUp()
		{
			EntityManager.RemoveComponent(m_LoopEntity, typeof(TGameMode));
		}

		public void LoadMap()
		{
			var ent = EntityManager.CreateEntity(typeof(RequestMapLoad));
			EntityManager.SetComponentData(ent, new RequestMapLoad {Key = new NativeString512("testvs")});
		}
	}

	public class GameModeContext : ExternalContextBase
	{
		public readonly MapManager MapMgr;

		public GameModeContext(MapManager mapMgr)
		{
			MapMgr = mapMgr;
		}

		public bool IsMapLoaded => MapMgr.IsMapLoaded;
	}

	[UpdateInGroup(typeof(GameModeSystemGroup))]
	public abstract class GameModeAsyncSystem<TGameMode> : GameBaseSystem
		where TGameMode : struct, IGameMode
	{
		private EntityQuery m_GameModeQuery;
		private EntityQuery m_ExecutingMapQuery;
		private EntityQuery m_MapLoadQuery;

		private Entity m_LoopEntity;
		private Machine m_Machine;

		protected Machine Machine => m_Machine;
		protected MapManager MapManager { get; private set; }
		
		protected abstract void OnCreateMachine(ref Machine machine);
		protected abstract void OnLoop(Entity gameModeEntity);

		protected override void OnCreate()
		{
			base.OnCreate();

			MapManager = World.GetOrCreateSystem<MapManager>();

			m_GameModeQuery     = GetEntityQuery(typeof(TGameMode));
			m_ExecutingMapQuery = GetEntityQuery(typeof(ExecutingMapData));
			m_MapLoadQuery      = GetEntityQuery(typeof(OperationMapLoadTag));

			m_Machine = new Machine();
			m_Machine.AddContext(new WorldContext(World));
			m_Machine.AddContext(new GameModeContext(MapManager));
			OnCreateMachine(ref m_Machine);
		}

		protected override void OnUpdate()
		{
			if (m_GameModeQuery.CalculateEntityCount() == 0)
				return;

			m_LoopEntity = m_GameModeQuery.GetSingletonEntity();

			OnLoop(m_LoopEntity);
		}

		public bool IsInitialization()
		{
			return EntityManager.HasComponent(m_LoopEntity, typeof(GameModeOnFirstInit));
		}

		public void FinishInitialization()
		{
			EntityManager.RemoveComponent(m_LoopEntity, typeof(GameModeOnFirstInit));
		}

		public bool IsCleanUp()
		{
			return !EntityManager.HasComponent(m_LoopEntity, typeof(ExecutingGameMode));
		}

		public void FinishCleanUp()
		{
			EntityManager.RemoveComponent(m_LoopEntity, typeof(TGameMode));
		}

		public void LoadMap()
		{
			var ent = EntityManager.CreateEntity(typeof(RequestMapLoad));
			EntityManager.SetComponentData(ent, new RequestMapLoad {Key = new NativeString512("testvs")});
		}
	}
}