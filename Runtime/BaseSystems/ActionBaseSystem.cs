using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public struct ActionShootHelper
	{
		public LocalToWorld Transform;
		public EyePosition  EyePosition;
		public AimLookState AimLook;

		public ActionShootHelper(LocalToWorld transform, EyePosition eyePosition, AimLookState aimLook)
		{
			Transform   = transform;
			EyePosition = eyePosition;
			AimLook     = aimLook;
		}

		public float3 GetPosition()
		{
			return Transform.Position + EyePosition.Value;
		}

		public float3 GetDirectionWithAimDelta(float2 delta)
		{
			var aim = AimLook.Aim;
			return Quaternion.Euler(-aim.y, aim.x, 0) * Quaternion.Euler(-delta.y, delta.x, 0) * new float3(0, 0, 1);
		}

		public void GetPositionAndDirection(out float3 position, out float3 direction)
		{
			position  = GetPosition();
			direction = GetDirectionWithAimDelta(float2.zero);
		}
	}

	[Obsolete("ActionBaseSystem should not be used anymore", true)]
	public abstract class ActionBaseSystem : JobGameBaseSystem
	{
		protected void GetPosition(in Entity livable, out float3 position)
		{
			position = new ActionShootHelper
			(
				EntityManager.GetComponentData<LocalToWorld>(livable),
				EntityManager.GetComponentData<EyePosition>(livable),
				default
			).GetPosition();
		}

		protected void GetDirectionWithAimDelta(in Entity livable, in float2 delta, out float3 direction)
		{
			direction = new ActionShootHelper
			(
				default,
				default,
				EntityManager.GetComponentData<AimLookState>(livable)
			).GetDirectionWithAimDelta(delta);
		}

		protected void GetPositionAndDirection(in Entity livable, out float3 position, out float3 direction)
		{
			new ActionShootHelper
			(
				EntityManager.GetComponentData<LocalToWorld>(livable),
				EntityManager.GetComponentData<EyePosition>(livable),
				EntityManager.GetComponentData<AimLookState>(livable)
			).GetPositionAndDirection(out position, out direction);
		}
	}
}