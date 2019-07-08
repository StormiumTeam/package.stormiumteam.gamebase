using System;
using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using Runtime.Misc;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace StormiumTeam.GameBase
{
	public class AssetPool<T>
		where T : Object
	{
		private World                 m_SpawnWorld;
		private Func<AssetPool<T>, T> m_CreateFunction;

		private List<T>  m_Objects;
		private Queue<T> m_ObjectPool;

		public int LastDequeueIndex { get; private set; }

		public AssetPool(Func<AssetPool<T>, T> createFunc, World spawnWorld = null)
		{
			m_CreateFunction = createFunc;
			m_SpawnWorld     = spawnWorld;

			m_Objects    = new List<T>();
			m_ObjectPool = new Queue<T>();
		}

		public void Enqueue(T obj)
		{
			m_ObjectPool.Enqueue(obj);
		}

		public T Dequeue()
		{
			if (m_ObjectPool.Count == 0)
			{
				T obj;
				using (new SetTemporaryActiveWorld(m_SpawnWorld ?? World.Active))
				{
					obj = m_CreateFunction(this);
				}

				m_Objects.Add(obj);
				return obj;
			}

			return m_ObjectPool.Dequeue();
		}

		public void AddElements(int size)
		{
			for (var i = 0; i != size; i++)
			{
				Enqueue(Dequeue());
			}
		}

		public int IndexOf(T obj)
		{
			return m_Objects.IndexOf(obj);
		}
	}

	public class AsyncAssetPool<T>
		where T : Object
	{
		public string AssetId;

		private T Asset;

		private List<T>  m_Objects;
		private Queue<T> m_ObjectPool;

		private List<OnLoad> m_EventQueue;

		public delegate void OnLoad(T result);

		public AsyncAssetPool(string id)
		{
			AssetId = id;

			m_Objects    = new List<T>();
			m_ObjectPool = new Queue<T>();
			m_EventQueue = new List<OnLoad>();

			InternalAddAsset().Completed += handle =>
			{
				Asset = handle.Result;
				foreach (var onLoad in m_EventQueue)
				{
					onLoad(Asset);
				}

				m_EventQueue.Clear();
			};
		}

		public void Enqueue(T obj)
		{
			m_ObjectPool.Enqueue(obj);
		}

		public void Dequeue(OnLoad complete)
		{
			if (m_ObjectPool.Count == 0)
			{
				m_Objects.Add(Asset);

				complete(Asset);
				return;
			}

			var obj = m_ObjectPool.Dequeue();
			if (obj == null)
			{
				m_Objects.Add(Asset);
				complete(Asset);
				return;
			}

			complete(obj);
		}

		public void SafeUnload()
		{
			foreach (var obj in m_ObjectPool)
			{
				Object.Destroy(obj);
				m_Objects.Remove(obj);
			}

			m_ObjectPool.Clear();
		}

		private AsyncOperationHandle<T> InternalAddAsset()
		{
			if (Asset == null)
			{
				return Addressables.LoadAssetAsync<T>(AssetId);
			}

			Debug.LogError("???????????????");
			return new AsyncOperationHandle<T>();
		}
	}

	public abstract class RuntimeAssetPresentation<TMonoPresentation> : MonoBehaviour
		where TMonoPresentation : RuntimeAssetPresentation<TMonoPresentation>
	{
		public RuntimeAssetBackend<TMonoPresentation> Backend { get; protected set; }

		internal void SetBackend(RuntimeAssetBackend<TMonoPresentation> backend)
		{
			Backend = backend;

			OnBackendSet();
		}

		public virtual void OnBackendSet()
		{
		}

		public virtual void OnReset()
		{
		}
	}

	public abstract class RuntimeAssetBackend<TMonoPresentation> : MonoBehaviour
		where TMonoPresentation : RuntimeAssetPresentation<TMonoPresentation>
	{
		private AsyncAssetPool<GameObject> m_PresentationPool;
		private AssetPool<GameObject>      m_RootPool;

		public int  DestroyFlags;
		public bool DisableNextUpdate, ReturnToPoolOnDisable, ReturnPresentationToPoolNextFrame;

		public EntityManager DstEntityManager { get; private set; }
		public Entity        DstEntity        { get; private set; }

		public TMonoPresentation Presentation { get; protected set; }

		public virtual void OnReset()
		{
		}

		public virtual void OnPoolSet()
		{
		}

		public void SetFromPool(AsyncAssetPool<GameObject> pool, EntityManager targetEm = null, Entity targetEntity = default)
		{
			m_PresentationPool = pool;

			DstEntityManager = targetEm;
			DstEntity        = targetEntity;

			OnPoolSet();

			pool.Dequeue(OnCompletePoolDequeue);
		}

		public void SetRootPool(AssetPool<GameObject> rootPool)
		{
			m_RootPool = rootPool;
		}

		private void OnCompletePoolDequeue(GameObject result)
		{
			if (result == null)
			{
				//Debug.LogError($"(Error, {name}) -> reported status: {op.Status}");
				return;
			}

			var previousWorld = World.Active;
			if (DstEntityManager != null)
				World.Active = DstEntityManager.World;
			var opResult = Instantiate(result, transform, true);

			if (DstEntityManager == null)
			{
				SetPresentation(opResult);
				World.Active = previousWorld;
				return;
			}

			var gameObjectEntity = opResult.GetComponent<GameObjectEntity>();
			if (!gameObjectEntity)
			{
				gameObjectEntity = opResult.AddComponent<GameObjectEntity>();
			}

			World.Active = previousWorld;

			//DstEntityManager.SetOrAddComponentData(DstEntity, new SubModel(gameObjectEntity.Entity)); todo: this is something that should be handled by systems
			DstEntityManager.SetOrAddComponentData(gameObjectEntity.Entity, new ModelParent {Parent = DstEntity});

			var listeners = opResult.GetComponents<IOnModelLoadedListener>();
			foreach (var listener in listeners)
			{
				listener.React(DstEntity, DstEntityManager, gameObject);
			}

			SetPresentation(opResult);
		}

		public void SetSingleModel(string key, EntityManager targetEm = null, Entity targetEntity = default)
		{
			if (m_PresentationPool != null) throw new InvalidOperationException("This object is already using pooling, you can't switch to a single operation anymore.");

			var loadModel = GetComponent<LoadModelFromStringBehaviour>();
			if (!loadModel)
				loadModel = gameObject.AddComponent<LoadModelFromStringBehaviour>();

			if (targetEm != null && targetEntity != default)
			{
				DstEntityManager = targetEm;
				DstEntity        = targetEntity;

				loadModel.OnLoadSetSubModelFor(targetEm, targetEntity);
			}

			loadModel.AssetId    = key;
			loadModel.SpawnRoot  = transform;
			loadModel.OnComplete = SetPresentation;
		}

		private bool SetPresentation(GameObject presentation)
		{
			presentation.transform.localPosition = Vector3.zero;
			presentation.transform.localRotation = Quaternion.identity;
			presentation.transform.localScale    = Vector3.one;

			Presentation = presentation.GetComponent<TMonoPresentation>();
			Presentation.OnReset();
			Presentation.SetBackend(this);

			OnPresentationSet();

			return true;
		}

		protected virtual void Update()
		{
			if (ReturnPresentationToPoolNextFrame)
			{
				ReturnPresentationToPoolNextFrame = false;
				ReturnPresentationToPool();
			}

			if (!DisableNextUpdate)
				return;

			DisableNextUpdate = false;
			gameObject.SetActive(false);

			if (ReturnToPoolOnDisable)
			{
				ReturnToPoolOnDisable = false;
				m_RootPool.Enqueue(gameObject);
			}
		}

		public void AddEntityLink()
		{
			if (m_PresentationPool != null) throw new InvalidOperationException("AddEntityLink() can't be used if pooling is active.");

			gameObject.AddComponent<DestroyGameObjectOnEntityDestroyed>().SetTarget(DstEntityManager, DstEntity);
		}

		public void SetDestroyFlags(int value)
		{
			if (value > 0)
				throw new NotImplementedException();

			DestroyFlags = value;

			if (value == 0)
			{
				DisableNextUpdate                 = true;
				ReturnToPoolOnDisable             = true;
				ReturnPresentationToPoolNextFrame = true;
			}
			else
			{
				DisableNextUpdate                 = false;
				ReturnToPoolOnDisable             = false;
				ReturnPresentationToPoolNextFrame = false;
			}
		}

		public void ReturnPresentationToPool()
		{
			if (Presentation != null)
				m_PresentationPool.Enqueue(Presentation.gameObject);
		}

		public void Return(bool disable, bool returnPresentation)
		{
			if (returnPresentation)
			{
				ReturnPresentationToPoolNextFrame = false;
				ReturnPresentationToPool();
			}

			if (disable)
			{
				gameObject.SetActive(false);
			}

			DisableNextUpdate     = false;
			ReturnToPoolOnDisable = false;

			m_RootPool.Enqueue(gameObject);
		}

		public virtual void OnPresentationSet()
		{
		}
	}
}