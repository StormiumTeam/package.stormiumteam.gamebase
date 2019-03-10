using System;
using package.stormium.core;
using Stormium.Core;
using Unity.Entities;
using UnityEngine;

namespace Stormium.Core
{
	public struct StActionSlotInput : IComponentData
	{
		[SerializeField]
		private byte m_ActiveFlags;

		public bool IsActive
		{
			get => Convert.ToBoolean(m_ActiveFlags);
			set => m_ActiveFlags = Convert.ToByte(value);
		}

		public StActionSlotInput(bool active)
		{
			m_ActiveFlags = Convert.ToByte(active);
		}
	}

	[UpdateInGroup(typeof(STUpdateOrder.UO_ActionGrabInputs))]
	public class StActionInputFromSlotUpdateSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			ForEach((ref StActionSlotInput data, ref StActionSlot actionSlot, ref OwnerState<PlayerDescription> player) =>
			{
				var target = player.Target;
				
				if (!EntityManager.HasComponent<ActionUserCommand>(target))
				{
					Debug.Log("Has no ActionUserCommand: " + target);
					return;
				}

				var fireBuffer = EntityManager.GetBuffer<ActionUserCommand>(target);
				if (actionSlot.Value >= fireBuffer.Length)
					return;
				
				var fireState  = fireBuffer[actionSlot.Value];

				data.IsActive = fireState.IsActive;
			});
		}
	}
}