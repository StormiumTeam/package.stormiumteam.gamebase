using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using StormiumTeam.GameBase.Utility.Misc;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StormiumTeam.GameBase.Utility.Pooling
{
	public class AsyncAssetPool<T>
		where T : Object
	{
		public delegate void OnLoad(T result);

		private readonly List<OnLoad> m_EventQueue;

		private readonly Queue<T> m_ObjectPool;

		public AssetPath AssetPath;

		// Is the pool still valid?
		public bool IsValid;
		public T    LoadedAsset;

		public AsyncAssetPool(AssetPath assetPath)
		{
			AssetPath = assetPath;
			IsValid   = true;

			m_ObjectPool = new Queue<T>();
			m_EventQueue = new List<OnLoad>();

			InternalAddAsset().ContinueWith(handle =>
			{
				if (handle == null)
				{
					Debug.LogError($"Asset {AssetPath} does not exist");
					return;
				}

				LoadedAsset = handle;
				foreach (var onLoad in m_EventQueue) onLoad(Object.Instantiate(LoadedAsset));

				m_EventQueue.Clear();
			});
		}

		public AsyncAssetPool(T origin, AssetPath assetPath = default)
		{
			AssetPath = assetPath;
			IsValid   = true;

			m_ObjectPool = new Queue<T>();
			m_EventQueue = new List<OnLoad>();

			LoadedAsset = origin;
		}

		public void Enqueue(T obj)
		{
			m_ObjectPool.Enqueue(obj);
		}

		public void StopDequeue(OnLoad function)
		{
			while (m_EventQueue.Contains(function))
				m_EventQueue.Remove(function);
		}

		public void Dequeue(OnLoad complete)
		{
			if (m_ObjectPool.Count == 0)
			{
				if (LoadedAsset == null)
					m_EventQueue.Add(complete);
				else
					complete(Object.Instantiate(LoadedAsset));

				return;
			}

			var obj = m_ObjectPool.Dequeue();
			if (obj == null)
			{
				complete(Object.Instantiate(LoadedAsset));
				return;
			}

			complete(obj);
		}

		public void SafeUnload()
		{
			foreach (var obj in m_ObjectPool) Object.Destroy(obj);

			IsValid = false;
			m_ObjectPool.Clear();
		}

		private UniTask<T> InternalAddAsset()
		{
			if (LoadedAsset == null) return AssetManager.LoadAssetAsync<T>(AssetPath);

			Debug.LogError("???????????????");
			return default;
		}

		public void AddElements(int elem)
		{
			for (var i = 0; i != elem; i++)
				Dequeue(c =>
				{
					var go = c as GameObject;
					if (go) go.SetActive(false);

					Enqueue(c);
				});
		}
	}
}