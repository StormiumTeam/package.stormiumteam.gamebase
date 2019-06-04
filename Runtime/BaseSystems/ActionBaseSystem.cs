using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(ActionSystemGroup))]
	public abstract class ActionBaseSystem : JobGameBaseSystem
	{
		public struct ShootHelper
		{
			public LocalToWorld Transform;
			public EyePosition  EyePosition;
			public AimLookState AimLook;

			public ShootHelper(LocalToWorld transform, EyePosition eyePosition, AimLookState aimLook)
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
				return (Quaternion.Euler(-aim.y, aim.x, 0) * Quaternion.Euler(-delta.y, delta.x, 0)) * new float3(0, 0, 1);
			}

			public void GetPositionAndDirection(out float3 position, out float3 direction)
			{
				position  = GetPosition();
				direction = GetDirectionWithAimDelta(float2.zero);
			}
		}
		
		protected void GetPosition(in Entity livable, out float3 position)
		{
			Debug.Assert(EntityManager.HasComponent<EyePosition>(livable));
			Debug.Assert(EntityManager.HasComponent<LocalToWorld>(livable));

			position = new ShootHelper
			(
				EntityManager.GetComponentData<LocalToWorld>(livable),
				EntityManager.GetComponentData<EyePosition>(livable),
				default
			).GetPosition();
		}

		protected void GetDirectionWithAimDelta(in Entity livable, in float2 delta, out float3 direction)
		{
			Debug.Assert(EntityManager.HasComponent<AimLookState>(livable));

			direction = new ShootHelper
			(
				default,
				default,
				EntityManager.GetComponentData<AimLookState>(livable)
			).GetDirectionWithAimDelta(delta);
		}

		protected void GetPositionAndDirection(in Entity livable, out float3 position, out float3 direction)
		{
			Debug.Assert(EntityManager.HasComponent<EyePosition>(livable));
			Debug.Assert(EntityManager.HasComponent<AimLookState>(livable));
			Debug.Assert(EntityManager.HasComponent<LocalToWorld>(livable));

			new ShootHelper
			(
				EntityManager.GetComponentData<LocalToWorld>(livable),
				EntityManager.GetComponentData<EyePosition>(livable),
				EntityManager.GetComponentData<AimLookState>(livable)
			).GetPositionAndDirection(out position, out direction);
		}
	}
	
	
	public abstract class ActionBaseSystem<TSpawnRequest> : ActionBaseSystem
		where TSpawnRequest : struct
	{
		protected NativeList<TSpawnRequest> SpawnRequests;

		protected override void OnCreate()
		{
			base.OnCreate();

			SpawnRequests = new NativeList<TSpawnRequest>(8, Allocator.Persistent);
		}

		protected abstract void OnActionUpdate();
		protected abstract void FinalizeSpawnRequests();

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps.Complete();

			SpawnRequests.Clear();

			OnActionUpdate();
			FinalizeSpawnRequests();

			return inputDeps;
		}
	}
}