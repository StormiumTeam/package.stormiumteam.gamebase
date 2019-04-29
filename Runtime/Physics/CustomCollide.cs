using System;
using package.stormiumteam.shared;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using Math = Unity.Physics.Math;
using Ray = Unity.Physics.Ray;
using RaycastHit = Unity.Physics.RaycastHit;

namespace StormiumTeam.GameBase
{
	public unsafe struct CustomCollideCollection : IComponentData
	{
		public int    Count;
		public IntPtr DataPtr;

		public CustomCollideCollection(DynamicBuffer<CustomCollide> cw)
		{
			Count   = default;
			DataPtr = default;

			ConvertFrom(cw);
		}

		public CustomCollideCollection(UnsafeAllocationLength<CustomCollide> cc)
		{
			Count = cc.Length;
			DataPtr = new IntPtr(cc.Data);
		}

		public CustomCollideCollection(CustomCollide* ptr, int length = 1)
		{
			DataPtr = (IntPtr) ptr;
			Count = length;
		}

		public void ConvertFrom(DynamicBuffer<CustomCollide> cwBuffer)
		{
			Count   = cwBuffer.Length;
			DataPtr = new IntPtr(cwBuffer.GetUnsafePtr());
		}

		public NativeArray<CustomCollide> AsNativeArray()
		{
			return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<CustomCollide>((void*) DataPtr, Count, Allocator.Invalid);
		}

		public bool Valid()
		{
			return DataPtr != IntPtr.Zero;
		}
	}

	public unsafe struct CustomCollide : IBufferElementData
	{	
		public Entity Target;

		[NativeDisableUnsafePtrRestriction]
		public Collider* Collider;

		public int RigidBodyIndex;

		public RigidTransform WorldFromMotion;

		public CustomCollide(PhysicsCollider collider, LocalToWorld localToWorld)
		{
			Collider        = collider.ColliderPtr;
			WorldFromMotion = new RigidTransform(localToWorld.Value);

			RigidBodyIndex = -1;
			Target = default;
		}
	}

	public static unsafe class CustomCollideExtensions
	{
		public static ref CustomCollide GetElementFromRigidBody(this CustomCollideCollection buffer, int rigidBodyIndex)
		{
			var ptr    = (CustomCollide*) buffer.DataPtr;
			var length = buffer.Count;

			for (var i = 0; i != length; i++)
			{
				ref var cw = ref UnsafeUtilityEx.ArrayElementAsRef<CustomCollide>(ptr, i);
				if (cw.RigidBodyIndex == rigidBodyIndex)
					return ref cw;
			}

			throw new Exception("No rigidBody found as " + rigidBodyIndex);
		}

		public static bool CastRay<T>(this CustomCollideCollection buffer, in RaycastInput input, ref T collector) where T : struct, ICollector<RaycastHit>
		{
			if (!buffer.Valid())
				throw new InvalidOperationException();
				
			var ptr    = (CustomCollide*) buffer.DataPtr;
			var length = buffer.Count;

			var hadHit = false;
			for (var i = 0; i != length; i++)
			{
				ref var cw = ref UnsafeUtilityEx.ArrayElementAsRef<CustomCollide>(ptr, i);

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

		public static bool CastRay(this CustomCollideCollection buffer, in RaycastInput input, out RaycastHit closestHit)
		{
			var closestHitCollector = new ClosestHitCollector<RaycastHit>(1f);
			var hadHit              = CastRay(buffer, in input, ref closestHitCollector);

			closestHit = closestHitCollector.ClosestHit;
			return hadHit;
		}

		public static bool CastCollider<T>(this CustomCollideCollection buffer, in ColliderCastInput input, ref T collector) where T : struct, ICollector<ColliderCastHit>
		{
			if (!buffer.Valid())
				throw new InvalidOperationException();
			
			var ptr    = (CustomCollide*) buffer.DataPtr;
			var length = buffer.Count;

			var hadHit = false;
			for (var i = 0; i != length; i++)
			{
				ref var cw = ref UnsafeUtilityEx.ArrayElementAsRef<CustomCollide>(ptr, i);

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

		public static bool CastCollider(this CustomCollideCollection buffer, in ColliderCastInput input, out ColliderCastHit closestHit)
		{
			var closestHitCollector = new ClosestHitCollector<ColliderCastHit>(1.000001f);
			var hadHit              = CastCollider(buffer, in input, ref closestHitCollector);

			closestHit = closestHitCollector.ClosestHit;
			return hadHit;
		}

		public static bool CalculateDistance<T>(this CustomCollideCollection buffer, ColliderDistanceInput input, ref T collector) where T : struct, ICollector<DistanceHit>
		{
			if (!buffer.Valid())
				throw new InvalidOperationException();
			
			var ptr    = (CustomCollide*) buffer.DataPtr;
			var length = buffer.Count;

			var hadHit = false;
			for (var i = 0; i != length; i++)
			{
				ref var cw = ref UnsafeUtilityEx.ArrayElementAsRef<CustomCollide>(ptr, i);

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
				var numHits  = collector.NumHits;

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