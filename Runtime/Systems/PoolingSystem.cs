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
		private   AssetPool<GameObject>         m_BackendPool;
		protected GetAllBackendModule<TBackend> Module;

		private AsyncAssetPool<GameObject> m_PresentationPool;


		private EntityQuery                m_Query;
		public  AssetPool<GameObject>      BackendPool      => m_BackendPool;
		public  AsyncAssetPool<GameObject> PresentationPool => m_PresentationPool;

		protected abstract string AddressableAsset { get; }

		protected TBackend LastBackend { get; set; }

		protected virtual Type[] AdditionalBackendComponents { get; }

		protected abstract EntityQuery GetQuery();

		protected virtual void CreatePoolBackend(out AssetPool<GameObject> pool)
		{
			pool = new AssetPool<GameObject>(p =>
			{
				var go = new GameObject($"pooled={GetType().Name}", AdditionalBackendComponents ?? new Type[0]);
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

			GetModule(out Module);
			CreatePoolBackend(out m_BackendPool);
			CreatePoolPresentation(out m_PresentationPool);

			m_Query = GetQuery();
		}

		protected override void OnUpdate()
		{
			if (m_Query.IsEmptyIgnoreFilter)
			{
				OnStopRunning();
				return;
			}

			Module.TargetEntities = m_Query.ToEntityArray(Allocator.TempJob);
			Module.Update(default).Complete();
			Module.TargetEntities.Dispose();

			foreach (var backendWithoutModel in Module.BackendWithoutModel) ReturnBackend(EntityManager.GetComponentObject<TBackend>(backendWithoutModel));

			foreach (var entityWithoutBackend in Module.MissingTargets) SpawnBackend(entityWithoutBackend);
		}

		protected override void OnStopRunning()
		{
			base.OnStopRunning();

			Module.TargetEntities = m_Query.ToEntityArray(Allocator.TempJob);
			Module.Update(default).Complete();
			Module.TargetEntities.Dispose();
			foreach (var entityWithBackend in Module.AttachedBackendEntities)
			{
				ReturnBackend(EntityManager.GetComponentObject<TBackend>(entityWithBackend));
			}

			foreach (var backendWithoutModel in Module.BackendWithoutModel)
			{
				ReturnBackend(EntityManager.GetComponentObject<TBackend>(backendWithoutModel));
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
			backend.OnReset();
			backend.SetTarget(EntityManager, target);
			backend.SetPresentationFromPool(m_PresentationPool);

			LastBackend = backend;
		}
	}
}