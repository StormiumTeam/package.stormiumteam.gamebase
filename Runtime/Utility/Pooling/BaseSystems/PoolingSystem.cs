﻿using System;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Modules;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Misc;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Utility.Pooling.BaseSystems
{
	public abstract class PoolingSystem<TBackend, TPresentation> : PoolingSystem<TBackend, TPresentation, GetAllBackendModule.AlwaysValid>
		where TBackend : RuntimeAssetBackendBase
		where TPresentation : RuntimeAssetPresentation
	{
	}

	public abstract class PoolingSystem<TBackend, TPresentation, TCheckValidity> : AbsGameBaseSystem
		where TBackend : RuntimeAssetBackendBase
		where TPresentation : RuntimeAssetPresentation
		where TCheckValidity : struct, ICheckValidity
	{
		private AssetPool<GameObject> m_BackendPool;

		private AsyncAssetPool<GameObject> m_PresentationPool;


		private   EntityQuery                                   m_Query;
		protected GetAllBackendModule<TBackend, TCheckValidity> Module;
		public    AssetPool<GameObject>                         BackendPool      => m_BackendPool;
		public    AsyncAssetPool<GameObject>                    PresentationPool => m_PresentationPool;

		public int PoolingVersion { get; private set; }

		protected abstract AssetPath AddressableAsset { get; }

		protected TBackend LastBackend { get; set; }

		protected virtual Type[] AdditionalBackendComponents { get; }
		protected         bool   RemoveFromDisabled          => true;

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
			if (AddressableAsset.IsCreated == false)
				throw new NullReferenceException($"{nameof(AddressableAsset)} is null, did you mean to replace 'CreatePoolPresentation' ?");

			if (AddressableAsset.IsEmpty == false)
				pool = new AsyncAssetPool<GameObject>(AddressableAsset);
			else
				pool = null;
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
			Module.Update();
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
			foreach (var entityWithBackend in Module.AttachedBackendEntities) ReturnBackend(EntityManager.GetComponentObject<TBackend>(entityWithBackend));

			foreach (var backendWithoutModel in Module.BackendWithoutModel) ReturnBackend(EntityManager.GetComponentObject<TBackend>(backendWithoutModel));
		}

		protected virtual void ReturnBackend(TBackend backend)
		{
			PoolingVersion++;
			backend.Return(true, true, RemoveFromDisabled);
		}

		protected virtual void SpawnBackend(Entity target)
		{
			PoolingVersion++;

			var gameObject = m_BackendPool.Dequeue();
			gameObject.SetActive(true);

			gameObject.name = $"{target} '{GetType().Name}' Backend";

			var backend = gameObject.GetComponent<TBackend>();
			backend.OnReset();
			backend.SetTarget(EntityManager, target);
			if (AddressableAsset.IsEmpty == false)
				backend.SetPresentationFromPool(m_PresentationPool);

			if (!EntityManager.Exists(backend.BackendEntity))
				Debug.LogError($"no entity ???? {backend.BackendEntity} <-> {backend.GetComponent<GameObjectEntity>().Entity}, {backend.GetComponent<GameObjectEntity>().World?.Name} <-> {World.Name}");
			EntityManager.AddSharedComponentData(backend.BackendEntity, new CreatedBySystem { System = this.GetType() });

			LastBackend = backend;
		}
	}
}