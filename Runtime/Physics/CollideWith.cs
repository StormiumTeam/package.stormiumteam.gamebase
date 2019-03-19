using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using Ray = Unity.Physics.Ray;
using RaycastHit = Unity.Physics.RaycastHit;

namespace StormiumTeam.GameBase
{
	public unsafe struct CollideWith : IBufferElementData
	{
		public Entity Target;

		[NativeDisableUnsafePtrRestriction]
		public Collider* Collider;

		public RigidTransform WorldFromMotion;
	}

	public static unsafe class CollideWithExtensions
	{
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
					collector.TransformNewHits(numHits, fraction, worldFromMotion, -1);
					hadHit = true;

					if (collector.EarlyOutOnFirstHit)
						return true;
				}
			}

			return hadHit;
		}

		public static bool CastRay(this DynamicBuffer<CollideWith> buffer, in RaycastInput input, out RaycastHit closestHit)
		{
			var closestHitCollector = new ClosestHitCollector<RaycastHit>(1.000001f);
			var hadHit              = CastRay(buffer, in input, ref closestHitCollector);

			closestHit = closestHitCollector.ClosestHit;
			return hadHit;
		}
	}
}