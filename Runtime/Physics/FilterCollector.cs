using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Physics;
using UnityEngine;
using UnityEngine.Assertions;
using Math = Unity.Physics.Math;
using RaycastHit = Unity.Physics.RaycastHit;

namespace StormiumTeam.GameBase
{
	// A collector which stores only the closest hit.
	public struct ClosestHitFilterCollector<T> : ICollector<T> where T : struct, IQueryResult
	{
		public bool  EarlyOutOnFirstHit => false;
		public float MaxFraction        { get; private set; }
		public int   NumHits            { get; private set; }

		private readonly NativeArray<int> m_Filters;
		private readonly bool m_FiltersCreated;
		
		private T                m_ClosestHit;
		public  T                ClosestHit => m_ClosestHit;

		public ClosestHitFilterCollector(NativeArray<int> filters, float maxFraction)
		{
			MaxFraction  = maxFraction;
			m_ClosestHit = default(T);
			m_Filters    = filters;
			m_FiltersCreated = filters.IsCreated;
			NumHits      = 0;
		}

		#region ICollector

		public unsafe bool AddHit(T hit)
		{
			if (m_FiltersCreated)
			{
				var rigidBodyIndex = -1;
				var sizeT = UnsafeUtility.SizeOf<T>();
				
				Debug.Log(sizeT + ", " + UnsafeUtility.SizeOf<ColliderCastHit>());
				
				if (sizeT == UnsafeUtility.SizeOf<RaycastHit>())
				{
					UnsafeUtility.CopyPtrToStructure(UnsafeUtility.AddressOf(ref hit), out RaycastHit output);
					rigidBodyIndex = output.RigidBodyIndex;
				}
				else if (sizeT == UnsafeUtility.SizeOf<ColliderCastHit>())
				{
					UnsafeUtility.CopyPtrToStructure(UnsafeUtility.AddressOf(ref hit), out ColliderCastHit output);
					rigidBodyIndex = output.RigidBodyIndex;
				}
				else if (sizeT == UnsafeUtility.SizeOf<DistanceHit>())
				{
					UnsafeUtility.CopyPtrToStructure(UnsafeUtility.AddressOf(ref hit), out DistanceHit output);
					rigidBodyIndex = output.RigidBodyIndex;
				}
				else if (sizeT == UnsafeUtility.SizeOf<OverlapAabbHit>())
				{
					UnsafeUtility.CopyPtrToStructure(UnsafeUtility.AddressOf(ref hit), out OverlapAabbHit output);
					rigidBodyIndex = output.RigidBodyIndex;
				}
				
				Debug.Log(rigidBodyIndex);

				if (rigidBodyIndex == -1 || !m_Filters.Contains(rigidBodyIndex))
					return false;
			}

			Assert.IsTrue(hit.Fraction <= MaxFraction);
			MaxFraction  = hit.Fraction;
			m_ClosestHit = hit;
			NumHits      = 1;
			return true;
		}

		public void TransformNewHits(int oldNumHits, float oldFraction, Math.MTransform transform, uint numSubKeyBits, uint subKey)
		{
			if (m_ClosestHit.Fraction < oldFraction)
			{
				m_ClosestHit.Transform(transform, numSubKeyBits, subKey);
			}
		}

		public void TransformNewHits(int oldNumHits, float oldFraction, Math.MTransform transform, int rigidBodyIndex)
		{
			if (m_ClosestHit.Fraction < oldFraction)
			{
				m_ClosestHit.Transform(transform, rigidBodyIndex);
			}
		}

		#endregion
	}
}