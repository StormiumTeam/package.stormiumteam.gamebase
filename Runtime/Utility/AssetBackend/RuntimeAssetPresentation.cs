using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Utility.AssetBackend
{
	[RequireComponent(typeof(GameObjectEntity))] // todo: use the new Converting system
	public abstract class RuntimeAssetPresentation<TMonoPresentation> : MonoBehaviour
		where TMonoPresentation : RuntimeAssetPresentation<TMonoPresentation>
	{
		private List<IBackendReceiver>                 m_Receivers;
		public  RuntimeAssetBackend<TMonoPresentation> Backend { get; protected set; }

		internal void SetBackend(RuntimeAssetBackend<TMonoPresentation> backend)
		{
			Backend     = backend;
			m_Receivers = new List<IBackendReceiver>();
			foreach (var receiver in GetComponentsInChildren<IBackendReceiver>())
			{
				receiver.Backend = backend;
				m_Receivers.Add(receiver);
			}

			OnBackendSet();
		}

		public virtual void OnBackendSet()
		{
			if (TryGetComponent(out GameObjectEntity goEntity)
			    && goEntity.Entity != default)
			{
#if UNITY_EDITOR
				goEntity.EntityManager.SetName(goEntity.Entity, $"Presentation '{GetType().Name}'");
#endif
				goEntity.EntityManager.SetOrAddComponentData(goEntity.Entity, GameObjectConversionUtility.GetEntityGuid(gameObject, 0));
			}

			foreach (var r in m_Receivers)
				r.OnBackendSet();
		}

		public virtual void OnReset()
		{
		}

		/// <summary>
		///     Call this method from a system...
		/// </summary>
		public virtual void OnSystemUpdate()
		{
			foreach (var r in m_Receivers)
				r.OnPresentationSystemUpdate();
		}
	}
}