using System;
using Stormium.Core;
using StormiumShared.Core.Networking;
using Unity.Entities;
using UnityEngine;

namespace Stormium.Core
{
	public struct GameEvent : IComponentData
	{
		public long Tick;
		public int Snapshot;
	}

	public class GameEventModelInfo
	{
		public void Create()
		{
			
		}
	}
	
	public interface IEventData : IComponentData
	{}
	
	[UpdateInGroup(typeof(STUpdateOrder.UO_EventManager))]
	public class GameEventManager : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			ForEach((Entity entity, ref GameEvent gameEvent) =>
			{
				if (EntityManager.HasComponent<ModelIdent>(entity))
				{
					Debug.Log("Destroyed event. " + EntityManager.GetComponentData<ModelIdent>(entity).Id);
				}

				PostUpdateCommands.DestroyEntity(entity);
			});
		}

		public void Create(EntityCommandBuffer commandBuffer)
		{
			commandBuffer.CreateEntity();
			commandBuffer.AddComponent(new GameEvent {Tick = this.Tick, Snapshot = 0});
		}

		/*public GameEventModelInfo RegisterEvent(string name, ComponentType[] components, EntityModelManager.WriteModel writeFunc, EntityModelManager.ReadModel readFunc)
		{
			EntityModelMgr.RegisterFull(name, components, null, null, writeFunc, readFunc);
		*/
	}
}