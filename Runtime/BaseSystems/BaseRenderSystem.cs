using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace StormiumTeam.GameBase.Systems
{
	public abstract class BaseRenderSystem<TDefinition> : GameBaseSystem
		where TDefinition : Component
	{
		protected abstract void PrepareValues();
		protected abstract void Render(TDefinition definition);
		protected abstract void ClearValues();

		protected override void OnUpdate()
		{
			// Prepare the values that is needed for the UI elements...
			PrepareValues();
			// Process the UI elements...
			Entities.ForEach((TDefinition definition) => { Render(definition); });
			// Clear values that were prepared.
			ClearValues();
		}
	}

	public class OrderInterfaceSystemGroup : OrderSystemGroup
	{}
	
	public abstract class OrderSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
			foreach (var componentSystemBase in Systems)
			{
				var system = (OrderingSystem) componentSystemBase;
				system.IncreaseOrder();
			}
		}
	}
	
	public abstract class OrderingSystem : ComponentSystem
	{
		public int Order { get; private set; }

		public void Reset()
		{
			Order = 0;
		}
		
		public void IncreaseOrder()
		{
			Order = Order += 1;
		}

		protected override void OnUpdate()
		{
		}
	}
}