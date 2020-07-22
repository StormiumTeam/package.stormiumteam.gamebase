using System;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.Utility.AssetBackend.Components;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Utility.AssetBackend
{
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

		public override void ReturnPresentation(bool unsetChildren = true)
		{
			if (unsetChildren)
			{
				foreach (var childPresentation in GetComponentsInChildren<RuntimeAssetBackendBase>())
				{
					if (childPresentation == this)
						continue;

					childPresentation.Return(true, true, true);
				}
			}

			ReturnPresentationToPool();
		}

		internal override void OnCompletePoolDequeue(GameObject result)
		{
			if (result == null)
			{
				Debug.Log($"got for '{name}' null presentation.");
				return;
			}

			var previousWorld = World.DefaultGameObjectInjectionWorld;
			if (DstEntityManager.World?.IsCreated == true)
				World.DefaultGameObjectInjectionWorld = DstEntityManager.World;

			var opResult = result;
			if (opResult.transform.parent != transform)
				opResult.transform.SetParent(transform, PresentationWorldTransformStayOnSpawn);
			opResult.SetActive(true);

			m_IncomingPresentation = false;

			if (DstEntityManager == default)
			{
				SetPresentation(opResult);
				World.DefaultGameObjectInjectionWorld = previousWorld;
				return;
			}

			var gameObjectEntity                    = opResult.GetComponent<GameObjectEntity>();
			if (!gameObjectEntity) gameObjectEntity = opResult.AddComponent<GameObjectEntity>();

			if (gameObjectEntity.EntityManager != DstEntityManager)
			{
				gameObjectEntity.enabled = false;
				gameObjectEntity.enabled = true;
			}

			World.DefaultGameObjectInjectionWorld = previousWorld;

			if (gameObjectEntity.Entity != default)
				DstEntityManager.SetOrAddComponentData(gameObjectEntity.Entity, new ModelParent {Parent = DstEntity});
			else
				Debug.LogWarning("Presentation gameObject entity is null, this may happen if the main gameObject is not active.\nPlease fix that behavior by calling gameObject.SetActive(true).");

			var listeners = opResult.GetComponents<IOnModelLoadedListener>();
			foreach (var listener in listeners) listener.React(DstEntity, DstEntityManager, gameObject);

			SetPresentation(opResult);
		}

		public override void SetSingleModel(string key, EntityManager targetEm = default, Entity targetEntity = default)
		{
			if (presentationPool != null) throw new InvalidOperationException("This object is already using pooling, you can't switch to a single operation anymore.");

			var loadModel = GetComponent<LoadModelFromStringIdBehaviour>();
			if (!loadModel)
				loadModel = gameObject.AddComponent<LoadModelFromStringIdBehaviour>();

			if (targetEm != default && targetEntity != default)
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
				tr.SetParent(null, PresentationWorldTransformStayOnSpawn);

				Presentation.gameObject.SetActive(false);

				if (presentationPool != null && presentationPool.IsValid)
				{
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