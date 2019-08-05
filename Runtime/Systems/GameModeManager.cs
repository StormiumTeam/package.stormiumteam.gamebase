using package.stormiumteam.shared.ecs;
using Unity.Entities;

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

		public void SetGameMode<T>(T data, string name = null)
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
		}

		public void SetGameMode<T>(Entity entity, string name = null)
			where T : struct, IGameMode
		{
			EntityManager.DestroyEntity(m_RunningGameMode);

			EntityManager.SetOrAddComponentData(entity, new GameModeOnFirstInit());
			EntityManager.SetOrAddComponentData(entity, new ExecutingGameMode
			{
				TypeIndex = ComponentType.ReadWrite<T>().TypeIndex,
				Name      = new NativeString64(name ?? typeof(T).Name)
			});
		}
	}

	[UpdateInGroup(typeof(GameModeSystemGroup))]
	public abstract class GameModeSystem<TGameMode> : GameBaseSystem
		where TGameMode : struct, IGameMode
	{
		private EntityQuery m_GameModeQuery;
		private Entity      m_LoopEntity;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_GameModeQuery = GetEntityQuery(typeof(ExecutingGameMode), typeof(TGameMode));
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
	}
}