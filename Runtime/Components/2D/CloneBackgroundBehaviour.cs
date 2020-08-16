using System;
using StormiumTeam.GameBase.Utility.Pooling;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace StormiumTeam.GameBase._2D
{
	[InternalBufferCapacity(16)]
	public struct ComputedBackground : IBufferElementData
	{
		public Entity                            child;
		public CloneBackgroundBehaviour.Computed data;

		public float position => data.position;
	}

	public class CloneBackgroundBehaviour : MonoBehaviour, IConvertGameObjectToEntity
	{
		[Flags]
		public enum EDirection
		{
			Left  = 1,
			Right = 2,
			Both  = Left | Right
		}

		public EDirection direction;
		public BackgroundAssetPresentation[] backgrounds;

		public struct Computed
		{
			public int   backgroundIndex;
			public float position;
		}

		public NativeArray<Computed> Compute(float position, float size, Allocator allocator)
		{
			var startPosition = transform.localPosition.x;
			var bg            = backgrounds[0]; // for now only do it with one background...

			var possible = (int) Math.Round(size / bg.width);
			possible = math.max(possible, 4);

			var array = new NativeArray<Computed>(possible, allocator);
			var half  = array.Length * 0.5f;
			for (var i = 0; i != array.Length; i++)
			{
				var computedPosition = startPosition + ((i - half) * bg.width) + ((int) (position / bg.width) * bg.width);

				array[i] = new Computed
				{
					backgroundIndex = 0,
					position        = computedPosition
				};
			}

			return array;
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.red;

			var cam = Camera.main;
			if (cam == null)
				return;

			using (var computedArray = Compute(cam.transform.position.x, cam.rect.width, Allocator.TempJob))
			{
				foreach (var c in computedArray)
					Gizmos.DrawWireCube(new Vector3(c.position, 10), new Vector3(backgrounds[c.backgroundIndex].width, 20));
			}
		}

		private AsyncAssetPool<GameObject> m_BackgroundPool;

		private void OnEnable()
		{
			m_BackgroundPool = new AsyncAssetPool<GameObject>(backgrounds[0].gameObject);
			m_BackgroundPool.AddElements(4);
			
			foreach (var bg in backgrounds)
				bg.gameObject.SetActive(false);
		}

		private void OnDisable()
		{
			m_BackgroundPool.SafeUnload();
			m_BackgroundPool = null;
		}

		public void SystemUpdate()
		{
			
		}

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponent(entity, typeof(BackgroundHolderTag));
			dstManager.AddComponentObject(entity, this);
			var buffer = dstManager.AddBuffer<ComputedBackground>(entity);
			buffer.Capacity += 1;
		}
	}
}