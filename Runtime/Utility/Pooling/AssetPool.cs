using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Utility.DOTS;
using StormiumTeam.GameBase.Utility.AssetBackend;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StormiumTeam.GameBase.Utility.Pooling
{
	public interface IAssetPool<T> : IDisposable
	{
		UniTask Warm();
		void    Enqueue(T                obj);
		void    Dequeue(OnAssetLoaded<T> onComplete);
	}

	public class AssetPool<T> : IAssetPool<T>
		where T : Object
	{
		private readonly Func<AssetPool<T>, T> m_CreateFunction;
		private readonly Queue<T>              m_ObjectPool;

		private readonly List<T> m_Objects;
		private readonly World   m_SpawnWorld;

		public AssetPool(Func<AssetPool<T>, T> createFunc, World spawnWorld = null)
		{
			m_CreateFunction = createFunc;
			m_SpawnWorld     = spawnWorld;

			m_Objects    = new List<T>();
			m_ObjectPool = new Queue<T>();
		}

		public int LastDequeueIndex { get; private set; }

		// There is no need to warm the Pool since it's not async
		public UniTask Warm()
		{
			return UniTask.CompletedTask;
		}

		public void Enqueue(T obj)
		{
			m_ObjectPool.Enqueue(obj);
		}

		public T Dequeue()
		{
			T obj = null;

			while (m_ObjectPool.Count > 0 && (obj = m_ObjectPool.Dequeue()) == null)
			{
			}

			if (obj != null)
				return obj;

			using (new SetTemporaryInjectionWorld(m_SpawnWorld ?? World.DefaultGameObjectInjectionWorld))
			{
				obj = m_CreateFunction(this);

				if (obj is GameObject go)
					foreach (var component1 in go.GetComponents(typeof(RuntimeAssetBackendBase)))
					{
						var component = (RuntimeAssetBackendBase) component1;
						if (component.rootPool == null)
							component.SetRootPool(this as AssetPool<GameObject>);
					}
			}

			m_Objects.Add(obj);
			return obj;
		}

		// this method is not public since we don't want caller of AssetPool to cause any GC alloc when dequeuing
		void IAssetPool<T>.Dequeue(OnAssetLoaded<T> complete) => complete(Dequeue());

		public void AddElements(int size)
		{
			for (var i = 0; i != size; i++) Enqueue(Dequeue());
		}

		public int IndexOf(T obj)
		{
			return m_Objects.IndexOf(obj);
		}
		
		public void SafeUnload()
		{
			foreach (var obj in m_ObjectPool) Object.Destroy(obj);
			
			m_ObjectPool.Clear();
		}

		public void Dispose()
		{
			SafeUnload();
		}
	}
}