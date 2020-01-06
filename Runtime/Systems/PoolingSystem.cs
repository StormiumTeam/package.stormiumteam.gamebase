using System;
using StormiumTeam.GameBase.Modules;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Systems
{
	public abstract class PoolingSystem<TBackend, TPresentation> : GameBaseSystem
		where TBackend : RuntimeAssetBackend<TPresentation>
		where TPresentation : RuntimeAssetPresentation<TPresentation>
	{
		private AssetPool<GameObject>         m_BackendPool;
		private GetAllBackendModule<TBackend> m_Module;

		private AsyncAssetPool<GameObject> m_PresentationPool;


		private EntityQuery                m_Query;
		public  AssetPool<GameObject>      BackendPool      => m_BackendPool;
		public  AsyncAssetPool<GameObject> PresentationPool => m_PresentationPool;

		protected abstract string AddressableAsset { get; }

		protected TBackend LastBackend { get; set; }

		protected abstract EntityQuery GetQuery();

		protected virtual void CreatePoolBackend(out AssetPool<GameObject> pool)
		{
			pool = new AssetPool<GameObject>(p =>
			{
				var go = new GameObject($"pooled={GetType().Name}");
				go.SetActive(false);

				go.AddComponent<TBackend>();
				go.AddComponent<GameObjectEntity>();

				return go;
			}, World);
		}

		protected virtual void CreatePoolPresentation(out AsyncAssetPool<GameObject> pool)
		{
			if (AddressableAsset == null)
				throw new NullReferenceException($"{nameof(AddressableAsset)} is null, did you mean to replace 'CreatePoolPresentation' ?");

			pool = new AsyncAssetPool<GameObject>(AddressableAsset);
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_Module);
			CreatePoolBackend(out m_BackendPool);
			CreatePoolPresentation(out m_PresentationPool);

			m_Query = GetQuery();
		}

		protected override void OnUpdate()
		{
			if (m_Query.IsEmptyIgnoreFilter)
				return;

			m_Module.TargetEntities = m_Query.ToEntityArray(Allocator.TempJob);
			m_Module.Update(default).Complete();
			m_Module.TargetEntities.Dispose();

			foreach (var backendWithoutModel in m_Module.BackendWithoutModel) ReturnBackend(EntityManager.GetComponentObject<TBackend>(backendWithoutModel));

			foreach (var entityWithoutBackend in m_Module.MissingTargets) SpawnBackend(entityWithoutBackend);
		}

		protected override void OnStopRunning()
		{
			base.OnStopRunning();

			m_Module.TargetEntities = m_Query.ToEntityArray(Allocator.TempJob);
			m_Module.Update(default).Complete();
			m_Module.TargetEntities.Dispose();
			foreach (var entityWithBackend in m_Module.AttachedBackendEntities)
			{
				ReturnBackend(EntityManager.GetComponentObject<TBackend>(entityWithBackend));
			}
		}

		protected virtual void ReturnBackend(TBackend backend)
		{
			backend.Return(true, true);
		}

		protected virtual void SpawnBackend(Entity target)
		{
			var gameObject = m_BackendPool.Dequeue();
			gameObject.SetActive(true);

			gameObject.name = $"{target} '{GetType().Name}' Backend";

			var backend = gameObject.GetComponent<TBackend>();
			backend.SetTarget(EntityManager, target);
			backend.SetPresentationFromPool(m_PresentationPool);

			LastBackend = backend;
		}
	}
}