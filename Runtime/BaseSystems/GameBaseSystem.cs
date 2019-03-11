using System.Collections.Generic;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;

namespace StormiumTeam.GameBase
{
	[InstanceSystem]
	public abstract class GameBaseSystem : BaseComponentSystem
	{		
		public GameManager       GameMgr        { get; private set; }
		public GameServerManager ServerMgr      { get; private set; }
		public EntityModelManager        EntityModelMgr { get; private set; }
		public GameTimeManager         TimeMgr        { get; private set; }
		public NetPatternSystem          PatternSystem  { get; private set; }
		public GameEventManager EventManager { get; private set; }

		public int Tick      => m_GameTimeSingletonGroup.GetSingleton<GameTimeComponent>().Value.Tick;
		public int TickDelta => m_GameTimeSingletonGroup.GetSingleton<GameTimeComponent>().Value.DeltaTick;

		public PatternBank LocalBank => PatternSystem.GetLocalBank();

		protected override void OnCreateManager()
		{			
			GameMgr        = World.GetOrCreateManager<GameManager>();
			ServerMgr      = World.GetOrCreateManager<GameServerManager>();	
			EntityModelMgr = World.GetOrCreateManager<EntityModelManager>();
			TimeMgr        = World.GetOrCreateManager<GameTimeManager>();
			PatternSystem  = World.GetOrCreateManager<NetPatternSystem>();
			EventManager = World.GetOrCreateManager<GameEventManager>();

			m_PlayerGroup = GetComponentGroup
			(
				typeof(GamePlayer)
			);

			m_GameTimeSingletonGroup = GetComponentGroup
			(
				typeof(GameTimeComponent)
			);
		}

		private ComponentGroup m_GameTimeSingletonGroup;
		private ComponentGroup m_PlayerGroup;

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
						if (playerArray[i].IsSelf == 1) return entityArray[i];
					}
				}
			}

			return default;
		}
	}

	[AlwaysUpdateSystem]
	public abstract class GameBaseSyncMessageSystem : GameBaseSystem
	{
		protected delegate void OnReceiveMessage(NetworkInstanceData networkInstance, Entity client, DataBufferReader data);
		
		private Dictionary<int, OnReceiveMessage> m_ActionForPattern;
		private ComponentGroup m_NetworkGroup;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();
			m_ActionForPattern = new Dictionary<int, OnReceiveMessage>();
			m_NetworkGroup = GetComponentGroup
			(
				typeof(NetworkInstanceData),
				typeof(NetworkInstanceToClient)
			);
		}

		protected override void OnUpdate()
		{
			var networkMgr    = World.GetExistingManager<NetworkManager>();
			var patternSystem = World.GetExistingManager<NetPatternSystem>();
			
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