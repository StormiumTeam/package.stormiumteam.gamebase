using StormiumShared.Core.Networking;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public struct GameEvent : IComponentData
	{
		public long Tick;
		public int Snapshot;
	}
	
	public interface IEventData : IComponentData
	{}
	
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
	}
}