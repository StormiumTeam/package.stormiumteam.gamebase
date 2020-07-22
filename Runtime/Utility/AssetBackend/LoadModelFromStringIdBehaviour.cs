using System;
using StormiumTeam.GameBase.Utility.AssetBackend.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace StormiumTeam.GameBase.Utility.AssetBackend
{
	public interface IOnModelLoadedListener
	{
		void React(Entity parentEntity, EntityManager entityManager, GameObject parentGameObject);
	}

	public class LoadModelFromStringIdBehaviour : MonoBehaviour
	{
		private string m_AssetId;

		private EntityManager m_EntityManager;
		private Entity        m_EntityToSubModel;

		private GameObject             m_Result;
		public  Func<GameObject, bool> OnComplete;

		public Transform SpawnRoot;

		public string AssetId
		{
			get => m_AssetId;
			set
			{
				if (m_AssetId == value)
					return;

				m_AssetId = value;
				Pop();
			}
		}

		private void OnEnable()
		{
			if (string.IsNullOrEmpty(m_AssetId)) return;

			Pop();
		}

		private void Pop()
		{
			Depop();

			Addressables.InstantiateAsync(m_AssetId, SpawnRoot).Completed += o =>
			{
				m_Result = o.Result;

				if (m_EntityManager.World?.IsCreated == false)
				{
					if (OnComplete?.Invoke(m_Result) == true) OnComplete = null;
					return;
				}

				var gameObjectEntity                    = m_Result.GetComponent<GameObjectEntity>();
				if (!gameObjectEntity) gameObjectEntity = m_Result.AddComponent<GameObjectEntity>();

				m_EntityManager.AddComponentData(gameObjectEntity.Entity, new ModelParent {Parent = m_EntityToSubModel});

				var listeners = m_Result.GetComponents<IOnModelLoadedListener>();
				foreach (var listener in listeners) listener.React(m_EntityToSubModel, m_EntityManager, gameObject);

				if (OnComplete?.Invoke(m_Result) == true) OnComplete = null;
			};
		}

		private void Depop()
		{
			if (m_Result)
				Addressables.ReleaseInstance(m_Result);

			m_Result = null;
		}

		private void OnDisable()
		{
			Depop();
		}

		public void OnLoadSetSubModelFor(EntityManager entityManager, Entity entity)
		{
			m_EntityManager    = entityManager;
			m_EntityToSubModel = entity;
		}
	}
}