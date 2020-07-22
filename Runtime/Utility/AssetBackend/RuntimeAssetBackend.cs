using System;
using DefaultNamespace.Utility.DOTS.xMonoBehaviour;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.Utility.AssetBackend.Components;
using StormiumTeam.GameBase.Utility.Pooling;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Utility.AssetBackend
{
	public abstract class RuntimeAssetBackendBase : MonoBehaviour
	{
		// only used to detect
		public class RuntimeAssetDetection : MonoBehaviour
		{
		}
		
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
		public abstract   void ReturnPresentation(bool unsetChildren = true);

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

			if (DstEntity == default || DstEntityManager.World?.IsCreated == false || gameObjectEntity == null)
				return;

			if (gameObjectEntity.EntityManager != DstEntityManager)
			{
				Debug.LogError($"'{gameObject.name}' have a different EntityManager than the destination. [go={gameObjectEntity.EntityManager.World.Name ?? "null"}, dst={DstEntityManager.World.Name ?? "null"}]");
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

		public void Return(bool disable, bool returnPresentation, bool unsetChildPresentations = false)
		{
			if (returnPresentation)
			{
				ReturnPresentationToPoolNextFrame = false;
				ReturnPresentation(unsetChildPresentations);
			}

			if (disable)
				gameObject.SetActive(false);

			DisableNextUpdate     = false;
			ReturnToPoolOnDisable = false;

			if (rootPool != null)
			{
				rootPool.Enqueue(gameObject);
				//transform.SetParent(null, false);
			}
		}

		public virtual void OnPresentationSet()
		{
		}
	}
}