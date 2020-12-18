using System;
using Cysharp.Threading.Tasks;
using StormiumTeam.GameBase.Utility.AssetBackend.Components;
using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Utility.AssetBackend
{
	public interface IOnModelLoadedListener
	{
		void React(Entity parentEntity, EntityManager entityManager, GameObject parentGameObject);
	}

	public class LoadModelFromStringIdBehaviour : MonoBehaviour
	{
		public string m_BundleId { get; private set; }
		public string m_AssetId  { get; private set; }

		private EntityManager m_EntityManager;
		private Entity        m_EntityToSubModel;

		private GameObject             m_Result;
		public  Func<GameObject, bool> OnComplete;

		public Transform SpawnRoot;

		public void SetAsset(in AssetPath assetPath)
		{
			SetAsset(assetPath.Bundle, assetPath.Asset);
		}

		public void SetAsset(string bundle, string asset)
		{
			if (m_BundleId == bundle && m_AssetId == asset)
				return;
			
			m_BundleId = bundle;
			m_AssetId  = asset;
			Pop();
		}

		private void OnEnable()
		{
			if (string.IsNullOrEmpty(m_AssetId)) return;

			Pop();
		}

		private void Pop()
		{
			Depop();

			AssetManager.LoadAssetAsync<GameObject>((m_BundleId, m_AssetId)).ContinueWith(go =>
			{
				if (go == null)
					throw new InvalidOperationException($"No asset named '{m_AssetId}' in bundle '{m_BundleId}' exists.");

				m_Result = go;
				m_Result.transform.SetParent(SpawnRoot);

				if (m_EntityManager.World?.IsCreated == false)
				{
					if (OnComplete?.Invoke(m_Result) == true) OnComplete = null;
					return;
				}

				var gameObjectEntity                    = m_Result.GetComponent<GameObjectEntity>();
				if (!gameObjectEntity) gameObjectEntity = m_Result.AddComponent<GameObjectEntity>();

				m_EntityManager.AddComponentData(gameObjectEntity.Entity, new ModelParent {Parent = m_EntityToSubModel});

				var listeners = m_Result.GetComponents<IOnModelLoadedListener>();
				foreach (var listener in listeners) listener.React(m_EntityToSubModel, m_EntityManager, go);

				if (OnComplete?.Invoke(m_Result) == true) OnComplete = null;
			});
		}

		private void Depop()
		{
			if (m_Result)
				AssetManager.Release(m_Result);

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