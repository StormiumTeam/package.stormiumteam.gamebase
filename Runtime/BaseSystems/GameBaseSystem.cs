using System;
using System.Collections.Generic;
using System.Linq;
using package.stormiumteam.networking.runtime.lowlevel;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public abstract class GameBaseSystem : BaseComponentSystem
	{
		public GameTime GameTime => m_GameTimeSingletonGroup.GetSingleton<GameTimeComponent>().Value;

		private ComponentSystemGroup m_ServerComponentGroup;

		public bool IsServer => m_ServerComponentGroup != null;

		protected override void OnCreate()
		{
			m_LocalPlayerGroup = GetEntityQuery
			(
				typeof(GamePlayer), typeof(GamePlayerLocalTag)
			);

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
		private EntityQuery m_LocalPlayerGroup;

		public void GetModule<TModule>(out TModule module)
			where TModule : BaseSystemModule, new()
		{
			module = new TModule();
			module.Enable(this);
		}

		public Entity GetFirstSelfGamePlayer()
		{
			if (m_LocalPlayerGroup.CalculateLength() > 0)
				return m_LocalPlayerGroup.GetSingletonEntity();

			return default;
		}

		public CameraState GetCurrentCameraState(Entity gamePlayer)
		{
			if (gamePlayer == default)
				return default;

			var serverCamera = EntityManager.GetComponentData<ServerCameraState>(gamePlayer);
			if (serverCamera.Mode == CameraMode.Forced)
				return serverCamera.Data;

			var localCamera = EntityManager.GetComponentData<LocalCameraState>(gamePlayer);
			if (localCamera.Mode == CameraMode.Forced)
				return localCamera.Data;

			return serverCamera.Data;
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

	public abstract class BaseSystemModule
	{
		public ComponentSystemBase System    { get; private set; }
		public bool                IsEnabled => System != null;

		public void Enable(ComponentSystemBase system)
		{
			System = system;
			OnEnable();
		}

		public JobHandle Update(JobHandle jobHandle)
		{
			if (!IsEnabled)
				throw new InvalidOperationException();

			OnUpdate(ref jobHandle);
			return jobHandle;
		}

		public void Disable()
		{
			OnDisable();
			System = null;
		}

		protected abstract void OnEnable();
		protected abstract void OnUpdate(ref JobHandle jobHandle);
		protected abstract void OnDisable();
	}

	public sealed class NetworkConnectionModule : BaseSystemModule
	{
		public EntityQuery        ConnectedQuery;
		public NativeList<Entity> ConnectedEntities;

		protected override void OnEnable()
		{
			ConnectedQuery = System.EntityManager.CreateEntityQuery(new EntityQueryDesc
			{
				All  = new[] {ComponentType.ReadWrite<NetworkIdComponent>()},
				None = new[] {ComponentType.ReadWrite<NetworkStreamDisconnected>()}
			});
			ConnectedEntities = new NativeList<Entity>(Allocator.Persistent);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
			ConnectedQuery.AddDependency(jobHandle);
			var connectionChunks = ConnectedQuery.CreateArchetypeChunkArray(Allocator.TempJob, out jobHandle);
			jobHandle.Complete();
			for (var chunk = 0; chunk != connectionChunks.Length; chunk++)
			{
				var entityArray = connectionChunks[chunk].GetNativeArray(System.GetArchetypeChunkEntityType());
				ConnectedEntities.AddRange(entityArray);
			}
		}

		protected override void OnDisable()
		{
			ConnectedEntities.Dispose();
		}
	}
}