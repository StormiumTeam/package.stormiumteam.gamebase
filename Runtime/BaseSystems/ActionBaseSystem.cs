using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace StormiumTeam.GameBase
{	
	[UpdateInGroup(typeof(ActionSystemGroup))]
	public abstract class ActionBaseSystem<TSpawnRequest> : GameBaseSystem
		where TSpawnRequest : struct
	{
		protected NativeList<TSpawnRequest> SpawnRequests;
		
		protected override void OnCreateManager()
		{
			base.OnCreateManager();
			
			SpawnRequests = new NativeList<TSpawnRequest>(8, Allocator.Persistent);
		}

		protected abstract void OnActionUpdate();
		protected abstract void FinalizeSpawnRequests();

		protected override void OnUpdate()
		{
			SpawnRequests.Clear();

			OnActionUpdate();
			FinalizeSpawnRequests();
		}

		protected void GetPosition(in Entity livable, out float3 position)
		{
			Debug.Assert(EntityManager.HasComponent<EyePosition>(livable));
			Debug.Assert(EntityManager.HasComponent<TransformState>(livable));

			position = EntityManager.GetComponentData<TransformState>(livable).Position
			           + EntityManager.GetComponentData<EyePosition>(livable).Value;
		}

		protected void GetDirectionWithAimDelta(in Entity livable, in float2 delta, out float3 direction)
		{
			var aim = EntityManager.GetComponentData<AimLookState>(livable).Aim + delta;
			direction = Quaternion.Euler(-aim.y, aim.x, 0) * Vector3.forward;
		}
		
		protected void GetPositionAndDirection(in Entity livable, out float3 position, out float3 direction)
		{
			Debug.Assert(EntityManager.HasComponent<EyePosition>(livable));
			Debug.Assert(EntityManager.HasComponent<AimLookState>(livable));
			Debug.Assert(EntityManager.HasComponent<TransformState>(livable));

			position = EntityManager.GetComponentData<TransformState>(livable).Position
			      + EntityManager.GetComponentData<EyePosition>(livable).Value;

			var aim = EntityManager.GetComponentData<AimLookState>(livable).Aim;
			direction = Quaternion.Euler(-aim.y, aim.x, 0) * Vector3.forward;
		}
	}
}