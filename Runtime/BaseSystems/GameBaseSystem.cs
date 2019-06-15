using System;
using System.Collections.Generic;
using System.Linq;
using package.stormiumteam.networking.runtime.lowlevel;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace StormiumTeam.GameBase
{
	public abstract class GameBaseSystem : BaseComponentSystem
	{
		public GameTime GameTime => m_GameTimeSingletonGroup.GetSingleton<GameTimeComponent>().Value;

		private ComponentSystemGroup m_ServerComponentGroup;

		public bool IsServer => m_ServerComponentGroup != null;

		protected override void OnCreate()
		{
			m_PlayerGroup = GetEntityQuery
			(
				typeof(GamePlayer)
			);

			m_GameTimeSingletonGroup = GetEntityQuery
			(
				typeof(GameTimeComponent)
			);

#if !UNITY_CLIENT
			m_ServerComponentGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
#endif
		}

		private EntityQuery m_GameTimeSingletonGroup;
		private EntityQuery m_PlayerGroup;

		public Entity GetFirstSelfGamePlayer()
		{
			var entityType = GetArchetypeChunkEntityType();
			var playerType = GetArchetypeChunkComponentType<GamePlayer>();

			using (var chunks = m_PlayerGroup.CreateArchetypeChunkArray(Allocator.TempJob))
			{
				foreach (var chunk in chunks)
				{
					var length = chunk.Count;

					var playerArray = chunk.GetNativeArray(playerType);
					var entityArray = chunk.GetNativeArray(entityType);
					for (var i = 0; i < length; i++)
					{
						if (playerArray[i].IsSelf) return entityArray[i];
					}
				}
			}

			return default;
		}

		public World GetActiveClientWorld()
		{
#if !UNITY_SERVER
#if !UNITY_EDITOR
			// There is only one client world outside of editor
			return ClientServerBootstrap.clientWorld == null ? null : ClientServerBootstrap.clientWorld[0];
#endif
			// There can be multiple client worlds inside the editor
			if (ClientServerBootstrap.clientWorld == null)
				return null;

			for (var i = 0; i != ClientServerBootstrap.clientWorld.Length; i++)
			{
				if (ClientServerBootstrap.clientWorld[i].GetExistingSystem<ClientPresentationSystemGroup>().Enabled)
					return ClientServerBootstrap.clientWorld[i];
			}
#endif

			return null;
		}
	}

	public abstract class JobGameBaseSystem : JobComponentSystem
	{
		public GameTime GameTime => m_GameTimeSingletonGroup.GetSingleton<GameTimeComponent>().Value;

		private ComponentSystemGroup m_ServerComponentGroup;

		public bool IsServer => m_ServerComponentGroup != null;

		protected override void OnCreate()
		{
			m_PlayerGroup = GetEntityQuery
			(
				typeof(GamePlayer)
			);

			m_GameTimeSingletonGroup = GetEntityQuery
			(
				typeof(GameTimeComponent)
			);

#if !UNITY_CLIENT
			m_ServerComponentGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
#endif
		}

		private EntityQuery m_GameTimeSingletonGroup;
		private EntityQuery m_PlayerGroup;

		public Entity GetFirstSelfGamePlayer()
		{
			var entityType = GetArchetypeChunkEntityType();
			var playerType = GetArchetypeChunkComponentType<GamePlayer>();

			using (var chunks = m_PlayerGroup.CreateArchetypeChunkArray(Allocator.TempJob))
			{
				foreach (var chunk in chunks)
				{
					var length = chunk.Count;

					var playerArray = chunk.GetNativeArray(playerType);
					var entityArray = chunk.GetNativeArray(entityType);
					for (var i = 0; i < length; i++)
					{
						if (playerArray[i].IsSelf) return entityArray[i];
					}
				}
			}

			return default;
		}

		protected bool RemoveFromServerWorld()
		{
			if (m_ServerComponentGroup == null)
				return false;

			if (m_ServerComponentGroup.Systems.Contains(this))
				m_ServerComponentGroup.RemoveSystemFromUpdateList(this);
			return true;
		}

		public World GetActiveClientWorld()
		{
#if !UNITY_SERVER
#if !UNITY_EDITOR
			// There is only one client world outside of editor
			return ClientServerBootstrap.clientWorld == null ? null : ClientServerBootstrap.clientWorld[0];
#endif
			// There can be multiple client worlds inside the editor
			if (ClientServerBootstrap.clientWorld == null)
				return null;

			for (var i = 0; i != ClientServerBootstrap.clientWorld.Length; i++)
			{
				if (ClientServerBootstrap.clientWorld[i].GetExistingSystem<ClientPresentationSystemGroup>().Enabled)
					return ClientServerBootstrap.clientWorld[i];
			}
#endif

			return null;
		}
	}
}