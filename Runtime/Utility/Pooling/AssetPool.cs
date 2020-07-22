using System;
using System.Collections.Generic;
using DefaultNamespace.Utility.DOTS;
using StormiumTeam.GameBase.Utility.AssetBackend;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StormiumTeam.GameBase.Utility.Pooling
{
	public class AssetPool<T>
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

			if (m_ObjectPool.Count > 0)
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

		public void AddElements(int size)
		{
			for (var i = 0; i != size; i++) Enqueue(Dequeue());
		}

		public int IndexOf(T obj)
		{
			return m_Objects.IndexOf(obj);
		}
	}
}