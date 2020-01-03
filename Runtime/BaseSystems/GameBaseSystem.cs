using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace StormiumTeam.GameBase
{
	public interface IGameBaseSystem
	{
		EntityQuery GetPlayerGroup();
	}
	
	public abstract class GameBaseSystem : BaseComponentSystem, IGameBaseSystem
	{
		private ClientSimulationSystemGroup m_ClientComponentGroup;
		private ComponentSystemGroup        m_ClientPresentationGroup;
		private EntityQuery                 m_LocalPlayerGroup;

		private EntityQuery                 m_PlayerGroup;
		private ServerSimulationSystemGroup m_ServerComponentGroup;

		public UTick ServerTick => GetTick(false);

		public bool IsServer             => m_ServerComponentGroup != null;
		public bool IsPresentationActive => m_ClientPresentationGroup != null && m_ClientPresentationGroup.Enabled;

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

		public void GetModule<TModule>(out TModule module)
			where TModule : BaseSystemModule, new()
		{
			module = new TModule();
			module.Enable(this);
		}

		EntityQuery IGameBaseSystem.GetPlayerGroup() => m_PlayerGroup;
	}

	public abstract class JobGameBaseSystem : JobComponentSystem, IGameBaseSystem
	{
		private ClientSimulationSystemGroup m_ClientComponentGroup;
		private ComponentSystemGroup        m_ClientPresentationGroup;
		private EntityQuery                 m_LocalPlayerGroup;

		private EntityQuery                 m_PlayerGroup;
		private ServerSimulationSystemGroup m_ServerComponentGroup;

		public UTick ServerTick => GetTick(true);

		public bool IsServer             => m_ServerComponentGroup != null;
		public bool IsPresentationActive => m_ClientPresentationGroup != null && m_ClientPresentationGroup.Enabled;

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

		public void GetModule<TModule>(out TModule module)
			where TModule : BaseSystemModule, new()
		{
			module = new TModule();
			module.Enable(this);
		}
		
		EntityQuery IGameBaseSystem.GetPlayerGroup() => m_PlayerGroup;
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
		public NativeList<Entity> ConnectedEntities;

		public          EntityQuery      ConnectedQuery;
		public override ModuleUpdateType UpdateType => ModuleUpdateType.All;

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
		public          List<BaseHandleDataPair> Handles;
		public override ModuleUpdateType         UpdateType => ModuleUpdateType.MainThread;

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

		public class BaseHandleDataPair
		{
			public AsyncOperationHandle Handle;
		}

		public class HandleDataPair<THandle, TData> : BaseHandleDataPair
			where TData : struct
		{
			public TData                         Data;
			public AsyncOperationHandle<THandle> Generic => Handle.Convert<THandle>();

			public void Deconstruct(out AsyncOperationHandle<THandle> handle, out TData data)
			{
				handle = Generic;
				data   = Data;
			}
		}
	}
}