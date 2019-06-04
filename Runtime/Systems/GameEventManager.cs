using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public struct GameEvent : IComponentData
	{
		public long Tick;
		public int  Snapshot;
	}

	public interface IEventData : IComponentData
	{
	}

	public class GameEventManager : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((Entity entity, ref GameEvent gameEvent) =>
			{
				if (EntityManager.HasComponent<ModelIdent>(entity))
				{
					Debug.Log("Destroyed event. " + EntityManager.GetComponentData<ModelIdent>(entity).Value);
				}

				PostUpdateCommands.DestroyEntity(entity);
			});
		}

		public void Create(EntityCommandBuffer commandBuffer)
		{
			var ent = commandBuffer.CreateEntity();
			commandBuffer.AddComponent(ent, new GameEvent {Tick = GameTime.Tick, Snapshot = 0});
		}
	}
}