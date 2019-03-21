using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using Math = Unity.Physics.Math;
using Ray = Unity.Physics.Ray;
using RaycastHit = Unity.Physics.RaycastHit;

namespace StormiumTeam.GameBase
{
	public unsafe struct CollideWith : IBufferElementData
	{
		public Entity Target;

		[NativeDisableUnsafePtrRestriction]
		public Collider* Collider;

		public int RigidBodyIndex;

		public RigidTransform WorldFromMotion;
	}

	public static unsafe class CollideWithExtensions
	{
		public static ref CollideWith GetElementFromRigidBody(this DynamicBuffer<CollideWith> buffer, int rigidBodyIndex)
		{
			var ptr    = buffer.GetUnsafePtr();
			var length = buffer.Length;

			for (var i = 0; i != length; i++)
			{
				ref var cw = ref UnsafeUtilityEx.ArrayElementAsRef<CollideWith>(ptr, i);
				if (cw.RigidBodyIndex == rigidBodyIndex)
					return ref cw;
			}

			throw new Exception("No rigidBody found as " + rigidBodyIndex);
		}

		public static bool CastRay<T>(this DynamicBuffer<CollideWith> buffer, in RaycastInput input, ref T collector) where T : struct, ICollector<RaycastHit>
		{
			var ptr    = buffer.GetUnsafePtr();
			var length = buffer.Length;

			var hadHit = false;
			for (var i = 0; i != length; i++)
			{
				ref var cw = ref UnsafeUtilityEx.ArrayElementAsRef<CollideWith>(ptr, i);

				var worldFromMotion = new Math.MTransform(cw.WorldFromMotion);
				var inputLs         = input;
				{
					var bodyFromWorld = Math.Inverse(worldFromMotion);
					var originLs      = Math.Mul(bodyFromWorld, input.Ray.Origin);
					var directionLs   = math.mul(bodyFromWorld.Rotation, input.Ray.Direction);
					inputLs.Ray = new Ray(originLs, directionLs);
				}

				var numHits  = collector.NumHits;
				var fraction = collector.MaxFraction;
				if (cw.Collider->CastRay(inputLs, ref collector))
				{
					collector.TransformNewHits(numHits, fraction, worldFromMotion, cw.RigidBodyIndex);
					hadHit = true;

					if (collector.EarlyOutOnFirstHit)
						return true;
				}
			}

			return hadHit;
		}

		public static bool CastRay(this DynamicBuffer<CollideWith> buffer, in RaycastInput input, out RaycastHit closestHit)
		{
			var closestHitCollector = new ClosestHitCollector<RaycastHit>(1f);
			var hadHit              = CastRay(buffer, in input, ref closestHitCollector);

			closestHit = closestHitCollector.ClosestHit;
			return hadHit;
		}

		public static bool CastCollider<T>(this DynamicBuffer<CollideWith> buffer, in ColliderCastInput input, ref T collector) where T : struct, ICollector<ColliderCastHit>
		{
			var ptr    = buffer.GetUnsafePtr();
			var length = buffer.Length;

			var hadHit = false;
			for (var i = 0; i != length; i++)
			{
				ref var cw = ref UnsafeUtilityEx.ArrayElementAsRef<CollideWith>(ptr, i);

				// Transform the input into body space
				var worldFromMotion = new Math.MTransform(cw.WorldFromMotion);
				var bodyFromWorld   = Math.Inverse(worldFromMotion);
				var inputLs = new ColliderCastInput
				{
					Collider    = input.Collider,
					Position    = Math.Mul(bodyFromWorld, input.Position),
					Orientation = math.mul(math.inverse(cw.WorldFromMotion.rot), input.Orientation),
					Direction   = math.mul(bodyFromWorld.Rotation, input.Direction)
				};

				var numHits  = collector.NumHits;
				var fraction = collector.MaxFraction;
				if (cw.Collider->CastCollider(inputLs, ref collector))
				{
					collector.TransformNewHits(numHits, fraction, worldFromMotion, cw.RigidBodyIndex);
					hadHit = true;
					
					if (collector.EarlyOutOnFirstHit)
						return true;
				}
			}

			return hadHit;
		}

		public static bool CastCollider(this DynamicBuffer<CollideWith> buffer, in ColliderCastInput input, out ColliderCastHit closestHit)
		{
			var closestHitCollector = new ClosestHitCollector<ColliderCastHit>(1.000001f);
			var hadHit              = CastCollider(buffer, in input, ref closestHitCollector);

			closestHit = closestHitCollector.ClosestHit;
			return hadHit;
		}

		public static bool CalculateDistance<T>(this DynamicBuffer<CollideWith> buffer, ColliderDistanceInput input, ref T collector) where T : struct, ICollector<DistanceHit>
		{
			var ptr    = buffer.GetUnsafePtr();
			var length = buffer.Length;

			var hadHit = false;
			for (var i = 0; i != length; i++)
			{
				ref var cw = ref UnsafeUtilityEx.ArrayElementAsRef<CollideWith>(ptr, i);

				// Transform the input into body space
				var worldFromMotion = new Math.MTransform(cw.WorldFromMotion);
				var bodyFromWorld   = Math.Inverse(worldFromMotion);
				var inputLs = new ColliderDistanceInput
				{
					Collider = input.Collider,
					Transform = new RigidTransform(
						math.mul(math.inverse(cw.WorldFromMotion.rot), input.Transform.rot),
						Math.Mul(bodyFromWorld, input.Transform.pos)
					),
					MaxDistance = input.MaxDistance
				};

				var fraction = collector.MaxFraction;
				var   numHits  = collector.NumHits;

				if (cw.Collider->CalculateDistance(inputLs, ref collector))
				{
					// Transform results back into world space
					collector.TransformNewHits(numHits, fraction, worldFromMotion, cw.RigidBodyIndex);
					hadHit = true;

					if (collector.EarlyOutOnFirstHit)
						return true;
				}
			}

			return hadHit;
		}
	}
}