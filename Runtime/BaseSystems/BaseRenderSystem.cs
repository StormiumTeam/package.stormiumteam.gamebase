using System.Linq;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
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
	{
	}

	public abstract class OrderSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
			var order = 0;
			foreach (var componentSystemBase in Systems)
			{
				var system = (OrderingSystem) componentSystemBase;
				system.SetOrder(order++);
			}
		}
	}

	public abstract class OrderingSystem : ComponentSystem
	{
		private bool m_Initialized;
		private int  m_Order;

		public int Order
		{
			get
			{
				if (!m_Initialized)
				{
					m_Initialized = true;
					foreach (var system in World.Systems)
					{
						if (system is OrderSystemGroup @group
						    && group.Systems.Contains(this))
						{
							system.Update();
						}
					}
				}

				return m_Order;
			}
		}

		public void Reset()
		{
			m_Initialized = true;
			m_Order       = 0;
		}

		public void SetOrder(int order)
		{
			m_Initialized = true;
			m_Order       = order;
		}

		protected override void OnUpdate()
		{
		}
	}
}