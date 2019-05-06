using System;
using System.Collections.Generic;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace StormiumTeam.GameBase
{
	[InstanceSystem]
	public abstract class GameBaseSystem : BaseComponentSystem
	{
		private JobHandle m_Dependency;
		private bool m_SystemGroupCanHaveDependency;

		private JobInternalSystem m_JobInternalSystem;
		
		public GameManager       GameMgr        { get; private set; }
		public GameServerManager ServerMgr      { get; private set; }
		public EntityModelManager        EntityModelMgr { get; private set; }
		public GameTimeManager         TimeMgr        { get; private set; }
		public NetPatternSystem          PatternSystem  { get; private set; }
		public GameEventManager EventManager { get; private set; }
		public PhysicQueryManager PhysicQueryManager { get; private set; }

		public int Tick      => m_GameTimeSingletonGroup.GetSingleton<SingletonGameTime>().Tick;
		public int TickDelta => m_GameTimeSingletonGroup.GetSingleton<SingletonGameTime>().DeltaTick;

		public PatternBank LocalBank => PatternSystem.GetLocalBank();

		protected override void OnCreate()
		{
			m_JobInternalSystem = World.GetOrCreateSystem<JobInternalSystem>();
			
			GameMgr        = World.GetOrCreateSystem<GameManager>();
			ServerMgr      = World.GetOrCreateSystem<GameServerManager>();	
			EntityModelMgr = World.GetOrCreateSystem<EntityModelManager>();
			TimeMgr        = World.GetOrCreateSystem<GameTimeManager>();
			PatternSystem  = World.GetOrCreateSystem<NetPatternSystem>();
			EventManager = World.GetOrCreateSystem<GameEventManager>();
			PhysicQueryManager = World.GetExistingSystem<PhysicQueryManager>();

			m_PlayerGroup = GetEntityQuery
			(
				typeof(GamePlayer)
			);

			m_GameTimeSingletonGroup = GetEntityQuery
			(
				typeof(SingletonGameTime)
			);
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

		public bool SystemGroup_CanHaveDependency()
		{
			return m_SystemGroupCanHaveDependency;
		}
		
		public void SystemGroup_CanHaveDependency(bool set)
		{
			m_SystemGroupCanHaveDependency = set;
		}

		public unsafe bool HasDependency()
		{
			if (!m_SystemGroupCanHaveDependency)
				return false;
			
			var jobGroup = *(IntPtr*) UnsafeUtility.AddressOf(ref m_Dependency);
			return jobGroup != IntPtr.Zero;
		}
		
		public JobHandle GetDependency()
		{
			return m_Dependency;
		}

		public void SetDependency(JobHandle v)
		{
			m_Dependency = v;
		}

		public void CompleteDependency()
		{
			m_Dependency.Complete();
			m_Dependency = default;
		}

		public BufferFromEntity<T> GetBufferFromEntity<T>() where T : struct, IBufferElementData
		{
			return m_JobInternalSystem.GetBufferFromEntity<T>();
		}
	}
	
	public class JobInternalSystem : JobComponentSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return inputDeps;
		}
	}

	[AlwaysUpdateSystem]
	public abstract class GameBaseSyncMessageSystem : GameBaseSystem
	{
		protected delegate void OnReceiveMessage(NetworkInstanceData networkInstance, Entity client, DataBufferReader data);
		
		private Dictionary<int, OnReceiveMessage> m_ActionForPattern;
		private EntityQuery m_NetworkGroup;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_ActionForPattern = new Dictionary<int, OnReceiveMessage>();
			m_NetworkGroup = GetEntityQuery
			(
				typeof(NetworkInstanceData),
				typeof(NetworkInstanceToClient)
			);
		}

		protected override void OnUpdate()
		{
			var networkMgr    = World.GetExistingSystem<NetworkManager>();
			var patternSystem = World.GetExistingSystem<NetPatternSystem>();
			
			using (var entityArray = m_NetworkGroup.ToEntityArray(Allocator.TempJob))
			using (var dataArray = m_NetworkGroup.ToComponentDataArray<NetworkInstanceData>(Allocator.TempJob))
			{
				for (var i = 0; i != entityArray.Length; i++)
				{
					var entity = entityArray[i];
					var data   = dataArray[i];
					if (data.InstanceType != InstanceType.LocalServer)
						continue;

					var evBuffer = EntityManager.GetBuffer<EventBuffer>(entity);
					for (var j = 0; j != evBuffer.Length; j++)
					{
						var ev = evBuffer[j].Event;
						if (ev.Type != NetworkEventType.DataReceived)
							continue;
						
						var foreignEntity = networkMgr.GetNetworkInstanceEntity(ev.Invoker.Id);
						var exchange      = patternSystem.GetLocalExchange(ev.Invoker.Id);
						var buffer        = BufferHelper.ReadEventAndGetPattern(ev, exchange, out var patternId);
						var clientEntity  = EntityManager.GetComponentData<NetworkInstanceToClient>(foreignEntity).Target;
						
						if (m_ActionForPattern.ContainsKey(patternId))
							m_ActionForPattern[patternId](data, clientEntity, new DataBufferReader(buffer, buffer.CurrReadIndex, buffer.Length));
					}
				}
			}
		}
		
		protected PatternResult AddMessage(OnReceiveMessage func, byte version = 0)
		{
			var patternName = $"auto.{GetType().Name}.{func.Method.Name}";
			var result      = LocalBank.Register(new PatternIdent(patternName, version));

			m_ActionForPattern[result.Id] = func;

			return result;
		}
		
		protected void SyncToServer(PatternResult result, DataBufferWriter syncData)
		{
			if (ServerMgr.ConnectedServerEntity == default)
				return;

			var instanceData = EntityManager.GetComponentData<NetworkInstanceData>(ServerMgr.ConnectedServerEntity);
			using (var buffer = BufferHelper.CreateFromPattern(result.Id, length: sizeof(byte) + sizeof(int) + syncData.Length))
			{
				buffer.WriteBuffer(syncData);
				
				instanceData.Commands.Send(buffer, default, Delivery.Reliable | Delivery.Unsequenced);
			}
		}
	}
}