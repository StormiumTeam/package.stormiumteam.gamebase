using package.stormiumteam.shared.ecs;
using Revolution.NetCode;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace StormiumTeam.GameBase.Misc
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class ClientCreateCameraSystem : GameBaseSystem
	{
		private Camera m_Camera;
		private bool   m_PreviousState;

		public Camera Camera => m_Camera;

		protected override void OnCreate()
		{
			base.OnCreate();

			GameObject gameObject;
			using (new SetTemporaryActiveWorld(World))
			{
				gameObject = new GameObject($"(World: {World.Name}) GameCamera",
					typeof(Camera),
					typeof(GameCamera),
					typeof(AudioListener),
					typeof(GameObjectEntity));
				m_Camera                  = gameObject.GetComponent<Camera>();
				m_Camera.orthographicSize = 10;
				m_Camera.fieldOfView      = 60;

				gameObject.transform.position = new Vector3(0, 0, -100);
			}

			gameObject.SetActive(false);
		}

		protected override void OnUpdate()
		{
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (m_Camera != null)
				GameObject.Destroy(m_Camera.gameObject);
			m_Camera = null;
		}

		internal void InternalSetActive(bool state)
		{
			if (state == m_PreviousState)
				return;

			m_PreviousState = state;

			using (new SetTemporaryActiveWorld(World))
			{
				m_Camera.gameObject.SetActive(state);
			}

			var e = m_Camera.GetComponent<GameObjectEntity>().Entity;
			if (state)
			{
				EntityManager.SetOrAddComponentData(e, new Translation());
				EntityManager.SetOrAddComponentData(e, new Rotation());
				EntityManager.SetOrAddComponentData(e, new LocalToWorld());
				EntityManager.SetOrAddComponentData(e, new CopyTransformToGameObject());
			}
		}
	}

	[UpdateBefore(typeof(TickClientPresentationSystem))]
	[NotClientServerSystem]
	[AlwaysUpdateSystem]
	public class ManageClientCameraSystem : GameBaseSystem
	{
		private EntityQuery m_GameCameraQuery;
		private Camera      m_Camera;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_GameCameraQuery = GetEntityQuery(typeof(GameCamera));
		}

		protected override void OnUpdate()
		{
			if (m_Camera == null && m_GameCameraQuery.CalculateEntityCount() > 0)
			{
				var entity = m_GameCameraQuery.GetSingletonEntity();
				m_Camera = EntityManager.GetComponentObject<Camera>(entity);
			}

			if (ClientServerBootstrap.clientWorld == null
			    || ClientServerBootstrap.clientWorld.Length <= 0)
			{
				if (m_Camera != null)
				{
					m_Camera.gameObject.SetActive(true);
				}

				return;
			}

			foreach (var clientWorld in ClientServerBootstrap.clientWorld)
			{
				var presentationSystemGroup = clientWorld.GetExistingSystem<ClientPresentationSystemGroup>();
				var cameraSystem            = clientWorld.GetExistingSystem<ClientCreateCameraSystem>();
				cameraSystem.InternalSetActive(presentationSystemGroup.Enabled);
			}

			if (m_Camera != null)
			{
				m_Camera.gameObject.SetActive(false);
			}
		}
	}
}