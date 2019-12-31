using System;
using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.Misc;
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
			if (m_ObjectPool.Count == 0)
			{
				T obj;
				using (new SetTemporaryActiveWorld(m_SpawnWorld ?? World.Active))
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

			return m_ObjectPool.Dequeue();
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

	public class AsyncAssetPool<T>
		where T : Object
	{
		public delegate void OnLoad(T result);

		public string AssetId;

		// Is the pool still valid?
		public bool IsValid;
		public T    LoadedAsset;

		private readonly List<OnLoad> m_EventQueue;

		private readonly Queue<T> m_ObjectPool;

		public AsyncAssetPool(string id)
		{
			AssetId = id;
			IsValid = true;

			m_ObjectPool = new Queue<T>();
			m_EventQueue = new List<OnLoad>();

			InternalAddAsset().Completed += handle =>
			{
				LoadedAsset = handle.Result;
				foreach (var onLoad in m_EventQueue) onLoad(Object.Instantiate(LoadedAsset));

				m_EventQueue.Clear();
			};
		}

		public AsyncAssetPool(T origin, string id = null)
		{
			AssetId = id;
			IsValid = true;

			m_ObjectPool = new Queue<T>();
			m_EventQueue = new List<OnLoad>();

			LoadedAsset = origin;
		}

		public void Enqueue(T obj)
		{
			m_ObjectPool.Enqueue(obj);
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

		private AsyncOperationHandle<T> InternalAddAsset()
		{
			if (LoadedAsset == null) return Addressables.LoadAsset<T>(AssetId);

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

	[RequireComponent(typeof(GameObjectEntity))] // todo: use the new Converting system
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

	public struct RuntimeAssetDisable : IComponentData
	{
		public static RuntimeAssetDisable All =>
			new RuntimeAssetDisable
			{
				IgnoreParent       = false,
				DisableGameObject  = true,
				ReturnToPool       = true,
				ReturnPresentation = true
			};

		public static RuntimeAssetDisable AllAndIgnoreParent =>
			new RuntimeAssetDisable
			{
				IgnoreParent       = true,
				DisableGameObject  = true,
				ReturnToPool       = true,
				ReturnPresentation = true
			};

		public bool IgnoreParent;
		public bool DisableGameObject;
		public bool ReturnToPool;
		public bool ReturnPresentation;
	}

	// only used to detect
	public class RuntimeAssetDetection : MonoBehaviour
	{
	}

	public abstract class RuntimeAssetBackendBase : MonoBehaviour
	{
		public int  DestroyFlags;
		public bool DisableNextUpdate, ReturnToPoolOnDisable, ReturnPresentationToPoolNextFrame;

		private   bool                       m_Enabled;
		protected bool                       m_IncomingPresentation;
		public    AsyncAssetPool<GameObject> presentationPool;
		public    AssetPool<GameObject>      rootPool;

		public EntityManager DstEntityManager { get; protected set; }
		public Entity        DstEntity        { get; protected set; }

		public Entity BackendEntity { get; private set; }

		public abstract object GetPresentationBoxed();

		internal abstract void OnCompletePoolDequeue(GameObject result);
		public abstract   void SetSingleModel(string            key, EntityManager em = default, Entity ent = default);
		internal abstract bool SetPresentation(GameObject       obj);
		public abstract   void ReturnPresentation();

		public virtual void OnComponentEnabled()
		{
		}

		public virtual void OnComponentDisabled()
		{
		}

		public virtual void OnReset()
		{
		}

		public virtual void OnTargetUpdate()
		{
		}

		public virtual void OnPresentationPoolUpdate()
		{
		}

		protected void UpdateGameObjectEntity()
		{
			Debug.Assert(m_Enabled, "m_Enabled");

			var gameObjectEntity                        = GetComponent<GameObjectEntity>();
			if (gameObjectEntity != null) BackendEntity = gameObjectEntity.Entity;

			if (DstEntity == default || DstEntityManager == null || gameObjectEntity == null)
				return;

			if (gameObjectEntity.EntityManager != DstEntityManager)
			{
				Debug.LogError($"'{gameObject.name}' have a different EntityManager than the destination. [go={gameObjectEntity.EntityManager?.World?.Name ?? "null"}, dst={DstEntityManager?.World?.Name ?? "null"}]");
				return;
			}

			if (!DstEntityManager.Exists(DstEntity))
			{
				Debug.LogError($"'{gameObject.name}' -> {DstEntityManager.World.Name} has no entity found with {DstEntity}'");
				return;
			}

			var entityManager = gameObjectEntity.EntityManager;
			entityManager.SetOrAddComponentData(gameObjectEntity.Entity, new ModelParent {Parent = DstEntity});
		}

		private void OnEnable()
		{
			m_Enabled = true;

			if (!GetComponent<RuntimeAssetDetection>())
				gameObject.AddComponent<RuntimeAssetDetection>();

			UpdateGameObjectEntity();

			OnComponentEnabled();
		}

		private void OnDisable()
		{
			m_Enabled = false;

			OnComponentDisabled();
			DstEntity        = default;
			DstEntityManager = default;
		}

		public void SetTarget(EntityManager entityManager, Entity target = default)
		{
			DstEntityManager = entityManager;
			DstEntity        = target;

			if (m_Enabled) UpdateGameObjectEntity();

			OnTargetUpdate();
		}

		public void SetPresentationFromPool(AsyncAssetPool<GameObject> pool)
		{
			presentationPool = pool;

			OnPresentationPoolUpdate();

			m_IncomingPresentation = true;
			pool.Dequeue(OnCompletePoolDequeue);
		}

		public void SetPresentationSingle(GameObject go)
		{
			presentationPool = null;
			OnPresentationPoolUpdate();

			m_IncomingPresentation = false;
			OnCompletePoolDequeue(go);
		}

		public void SetRootPool(AssetPool<GameObject> rootPool)
		{
			this.rootPool = rootPool;
		}

		protected virtual void Update()
		{
			if (ReturnPresentationToPoolNextFrame)
			{
				Debug.Log("return pp " + gameObject.name);

				ReturnPresentationToPoolNextFrame = false;
				ReturnPresentation();
			}

			if (!DisableNextUpdate)
				return;

			Debug.Log("return " + gameObject.name);

			DisableNextUpdate = false;
			gameObject.SetActive(false);

			if (ReturnToPoolOnDisable)
			{
				ReturnToPoolOnDisable = false;
				rootPool.Enqueue(gameObject);
			}
		}

		public void AddEntityLink()
		{
			if (presentationPool != null) throw new InvalidOperationException("AddEntityLink() can't be used if pooling is active.");

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

		public void ReturnDelayed(EntityCommandBuffer entityCommandBuffer, bool disable, bool returnPresentation)
		{
			DisableNextUpdate                 = disable;
			ReturnToPoolOnDisable             = true;
			ReturnPresentationToPoolNextFrame = returnPresentation;

			entityCommandBuffer.AddComponent(gameObject.GetComponent<GameObjectEntity>().Entity, new Disabled());
		}

		public void Return(bool disable, bool returnPresentation)
		{
			if (returnPresentation)
			{
				ReturnPresentationToPoolNextFrame = false;
				ReturnPresentation();
			}

			if (disable) gameObject.SetActive(false);

			DisableNextUpdate     = false;
			ReturnToPoolOnDisable = false;

			rootPool.Enqueue(gameObject);
		}

		public virtual void OnPresentationSet()
		{
		}
	}

	public abstract class RuntimeAssetBackend<TMonoPresentation> : RuntimeAssetBackendBase
		where TMonoPresentation : RuntimeAssetPresentation<TMonoPresentation>
	{
		public TMonoPresentation Presentation            { get; protected set; }
		public bool              HasIncomingPresentation => m_IncomingPresentation || Presentation != null;

		public virtual bool PresentationWorldTransformStayOnSpawn => true;
		
		public override object GetPresentationBoxed()
		{
			return Presentation;
		}

		public override void ReturnPresentation()
		{
			ReturnPresentationToPool();
		}

		internal override void OnCompletePoolDequeue(GameObject result)
		{
			if (result == null)
			{
				Debug.Log($"got for '{name}' null presentation.");
				return;
			}

			var previousWorld = World.Active;
			if (DstEntityManager != null)
				World.Active = DstEntityManager.World;

			var opResult = result;
			opResult.transform.SetParent(transform, PresentationWorldTransformStayOnSpawn);
			opResult.SetActive(true);

			m_IncomingPresentation = false;

			if (DstEntityManager == null)
			{
				SetPresentation(opResult);
				World.Active = previousWorld;
				return;
			}

			var gameObjectEntity                    = opResult.GetComponent<GameObjectEntity>();
			if (!gameObjectEntity) gameObjectEntity = opResult.AddComponent<GameObjectEntity>();

			World.Active = previousWorld;

			if (gameObjectEntity.Entity != default)
				DstEntityManager.SetOrAddComponentData(gameObjectEntity.Entity, new ModelParent {Parent = DstEntity});
			else
				Debug.LogWarning("Presentation gameObject entity is null, this may happen if the main gameObject is not active.\nPlease fix that behavior by calling gameObject.SetActive(true).");

			var listeners = opResult.GetComponents<IOnModelLoadedListener>();
			foreach (var listener in listeners) listener.React(DstEntity, DstEntityManager, gameObject);

			SetPresentation(opResult);
		}

		public override void SetSingleModel(string key, EntityManager targetEm = null, Entity targetEntity = default)
		{
			if (presentationPool != null) throw new InvalidOperationException("This object is already using pooling, you can't switch to a single operation anymore.");

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

		internal override bool SetPresentation(GameObject gameObject)
		{
			var tr = gameObject.transform;
			tr.localPosition = Vector3.zero;
			tr.localRotation = Quaternion.identity;
			tr.localScale    = Vector3.one;

			Presentation = gameObject.GetComponent<TMonoPresentation>();
			Presentation.OnReset();
			Presentation.SetBackend(this);

			m_IncomingPresentation = false;

			OnPresentationSet();

			return true;
		}

		public void ReturnPresentationToPool()
		{
			if (Presentation != null)
			{
				var tr = Presentation.transform;
				tr.parent = null;

				Presentation.gameObject.SetActive(false);

				if (presentationPool != null && presentationPool.IsValid)
				{
					Debug.Log("Enqueue " + Presentation.name);
					presentationPool.Enqueue(Presentation.gameObject);
				}
				else
				{
					Debug.Log("Null pool or not valid for " + Presentation.name);
					Destroy(Presentation.gameObject);
				}
			}

			Presentation = null;
		}
	}
}