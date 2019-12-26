using System;
using System.Collections.Generic;
using Unity.NetCode;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace StormiumTeam.GameBase
{
	public abstract class GameBaseSystem : BaseComponentSystem
	{
		private ServerSimulationSystemGroup m_ServerComponentGroup;
		private ClientSimulationSystemGroup m_ClientComponentGroup;
		private ComponentSystemGroup        m_ClientPresentationGroup;

		public UTick ServerTick => GetTick(false);

		public UTick GetTick(bool predicted)
		{
			var isClient = m_ClientComponentGroup != null;
			var isServer = m_ServerComponentGroup != null;
			if (!isClient && !isServer)
				throw new InvalidOperationException("Can only be called on client or server world.");

			return isClient
				? m_ClientComponentGroup.GetServerTick()
				: m_ServerComponentGroup.GetServerTick();
		}

		public bool IsServer             => m_ServerComponentGroup != null;
		public bool IsPresentationActive => m_ClientPresentationGroup != null && m_ClientPresentationGroup.Enabled;

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

#if !UNITY_CLIENT
			m_ServerComponentGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
#endif
#if !UNITY_SERVER
			m_ClientPresentationGroup = World.GetExistingSystem<ClientPresentationSystemGroup>();
			m_ClientComponentGroup    = World.GetExistingSystem<ClientSimulationSystemGroup>();
#endif
		}

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
			if (m_LocalPlayerGroup.CalculateEntityCount() > 0)
				return m_LocalPlayerGroup.GetSingletonEntity();

			return default;
		}

		public bool TryGetCurrentCameraState(Entity gamePlayer, out CameraState cameraState)
		{
			cameraState = default;
			if (gamePlayer == default)
				return false;

			var comps = EntityManager.GetChunk(gamePlayer).Archetype.GetComponentTypes();
			if (!comps.Contains(ComponentType.ReadWrite<ServerCameraState>()))
				return false;

			var serverCamera = EntityManager.GetComponentData<ServerCameraState>(gamePlayer);
			if (serverCamera.Mode == CameraMode.Forced || !comps.Contains(ComponentType.ReadWrite<LocalCameraState>()))
			{
				cameraState = serverCamera.Data;
				return true;
			}

			var localCamera = EntityManager.GetComponentData<LocalCameraState>(gamePlayer);
			if (localCamera.Mode == CameraMode.Forced)
			{
				cameraState = localCamera.Data;
				return true;
			}

			cameraState = serverCamera.Data;
			return true;
		}

		public World GetActiveClientWorld()
		{
#if UNITY_SERVER
			throw new Exeception("GetActiveClientWorld() shouldn't be called on server.");
#else
			foreach (var world in World.AllWorlds)
			{
				if (world.GetExistingSystem<ClientPresentationSystemGroup>()?.Enabled == true)
					return world;
			}

			return null;
#endif
		}
	}

	public abstract class JobGameBaseSystem : JobComponentSystem
	{
		private ServerSimulationSystemGroup m_ServerComponentGroup;
		private ClientSimulationSystemGroup m_ClientComponentGroup;
		private ComponentSystemGroup        m_ClientPresentationGroup;

		public UTick ServerTick => GetTick(false);

		public UTick GetTick(bool predicted)
		{
			var isClient = m_ClientComponentGroup != null;
			var isServer = m_ServerComponentGroup != null;
			if (!isClient && !isServer)
				throw new InvalidOperationException("Can only be called on client or server world.");

			return isClient
				? m_ClientComponentGroup.GetServerTick()
				: m_ServerComponentGroup.GetServerTick();
		}

		public bool IsServer             => m_ServerComponentGroup != null;
		public bool IsPresentationActive => m_ClientPresentationGroup != null && m_ClientPresentationGroup.Enabled;

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

#if !UNITY_CLIENT
			m_ServerComponentGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
#endif
#if !UNITY_SERVER
			m_ClientComponentGroup    = World.GetExistingSystem<ClientSimulationSystemGroup>();
			m_ClientPresentationGroup = World.GetExistingSystem<ClientPresentationSystemGroup>();
#endif
		}

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
			if (m_LocalPlayerGroup.CalculateEntityCount() > 0)
				return m_LocalPlayerGroup.GetSingletonEntity();

			return default;
		}

		public CameraState GetCurrentCameraState(Entity gamePlayer)
		{
			if (gamePlayer == default)
				return default;

			var comps = EntityManager.GetChunk(gamePlayer).Archetype.GetComponentTypes();
			if (!comps.Contains(ComponentType.ReadWrite<ServerCameraState>()))
				return default;

			var serverCamera = EntityManager.GetComponentData<ServerCameraState>(gamePlayer);
			if (serverCamera.Mode == CameraMode.Forced)
				return serverCamera.Data;

			if (!comps.Contains(ComponentType.ReadWrite<LocalCameraState>()))
				return serverCamera.Data;

			var localCamera = EntityManager.GetComponentData<LocalCameraState>(gamePlayer);
			if (localCamera.Mode == CameraMode.Forced)
				return localCamera.Data;

			return serverCamera.Data;
		}

		public World GetActiveClientWorld()
		{
#if UNITY_SERVER
			throw new Exeception("GetActiveClientWorld() shouldn't be called on server.");
#else
			foreach (var world in World.AllWorlds)
			{
				if (world.GetExistingSystem<ClientPresentationSystemGroup>()?.Enabled == true)
					return world;
			}

			return null;
#endif
		}
	}

	[Flags]
	public enum ModuleUpdateType
	{
		MainThread = 1,
		Job        = 2,
		All        = MainThread | Job
	}

	public abstract class BaseSystemModule
	{
		public virtual ModuleUpdateType UpdateType => ModuleUpdateType.MainThread;

		public ComponentSystemBase System    { get; private set; }
		public bool                IsEnabled => System != null;

		public void Enable(ComponentSystemBase system)
		{
			System = system;
			OnEnable();
		}

		public void Update()
		{
			if ((UpdateType & ModuleUpdateType.MainThread) == 0)
				throw new InvalidOperationException();

			if (!IsEnabled)
				throw new InvalidOperationException();

			var tmp = default(JobHandle);
			OnUpdate(ref tmp);
		}

		public JobHandle Update(JobHandle jobHandle)
		{
			if ((UpdateType & ModuleUpdateType.Job) == 0)
				throw new InvalidOperationException();

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
		public override ModuleUpdateType UpdateType => ModuleUpdateType.All;

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
			ConnectedEntities.Clear();
			ConnectedQuery.AddDependency(jobHandle);
			var connectionChunks = ConnectedQuery.CreateArchetypeChunkArray(Allocator.TempJob, out jobHandle);
			jobHandle.Complete();
			for (var chunk = 0; chunk != connectionChunks.Length; chunk++)
			{
				var entityArray = connectionChunks[chunk].GetNativeArray(System.GetArchetypeChunkEntityType());
				ConnectedEntities.AddRange(entityArray);
			}

			connectionChunks.Dispose();
		}

		protected override void OnDisable()
		{
			ConnectedEntities.Dispose();
		}
	}

	public sealed class AsyncOperationModule : BaseSystemModule
	{
		public override ModuleUpdateType UpdateType => ModuleUpdateType.MainThread;

		public class BaseHandleDataPair
		{
			public AsyncOperationHandle Handle;
		}

		public class HandleDataPair<THandle, TData> : BaseHandleDataPair
			where TData : struct
		{
			public AsyncOperationHandle<THandle> Generic => Handle.Convert<THandle>();
			public TData                         Data;

			public void Deconstruct(out AsyncOperationHandle<THandle> handle, out TData data)
			{
				handle = Generic;
				data   = Data;
			}
		}

		public List<BaseHandleDataPair> Handles;

		protected override void OnEnable()
		{
			Handles = new List<BaseHandleDataPair>(8);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{

		}

		protected override void OnDisable()
		{
			Handles.Clear();
		}

		public void Add<THandle, TData>(AsyncOperationHandle<THandle> handle, TData data)
			where TData : struct
		{
			Handles.Add(new HandleDataPair<THandle, TData>
			{
				Handle = handle,
				Data   = data
			});
		}

		public HandleDataPair<THandle, TData> Get<THandle, TData>(int index)
			where TData : struct
		{
			return (HandleDataPair<THandle, TData>) Handles[index];
		}
	}
}