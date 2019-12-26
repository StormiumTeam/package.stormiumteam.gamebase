using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Physics;

namespace StormiumTeam.GameBase
{
	public unsafe struct JobPhysicsQuery
	{
#pragma warning disable 649
		[NativeSetThreadIndex] private readonly int m_ThreadIndex;
#pragma warning restore 649

		[NativeDisableParallelForRestriction]
		private NativeArray<BlobAssetReference<Collider>> m_Colliders;

		public     BlobAssetReference<Collider> Blob      => m_Colliders[m_ThreadIndex];
		public ref Collider                     Reference => ref Blob.Value;

		public Collider* Ptr => (Collider*) Blob.GetUnsafePtr();

		public JobPhysicsQuery(Func<BlobAssetReference<Collider>> autoCreateFunc)
		{
			m_Colliders = new NativeArray<BlobAssetReference<Collider>>(JobsUtility.MaxJobThreadCount, Allocator.Persistent);
			for (var i = 0; i != JobsUtility.MaxJobThreadCount; i++) m_Colliders[i] = autoCreateFunc();

			m_ThreadIndex = 0;
		}

		public void Dispose()
		{
			for (var i = 0; i != m_Colliders.Length; i++) m_Colliders[i].Release();

			m_Colliders.Dispose();
		}
	}
}