using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace StormiumTeam.GameBase.Misc
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class ClientCreateCameraSystem : GameBaseSystem
	{
		private bool m_PreviousState;

		public Camera Camera { get; private set; }

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
				Camera                  = gameObject.GetComponent<Camera>();
				Camera.orthographicSize = 10;
				Camera.fieldOfView      = 60;
				Camera.nearClipPlane    = 0.025f;

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

			if (Camera != null)
				Object.Destroy(Camera.gameObject);
			Camera = null;
		}

		internal void InternalSetActive(bool state)
		{
			if (state == m_PreviousState)
				return;

			m_PreviousState = state;

			using (new SetTemporaryActiveWorld(World))
			{
				Camera.gameObject.SetActive(state);
			}

			var e = Camera.GetComponent<GameObjectEntity>().Entity;
			if (state)
			{
				EntityManager.SetOrAddComponentData(e, new Translation());
				EntityManager.SetOrAddComponentData(e, new Rotation());
				EntityManager.SetOrAddComponentData(e, new LocalToWorld());
				EntityManager.AddComponent(e, typeof(DefaultCamera));
			}
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.CopyToGameObject))]
	public class CopyCameraTranslationToTransform : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((GameCamera camera, ref Translation translation) =>
			{
				camera.transform.position = translation.Value;
				Debug.DrawRay(translation.Value + new float3(0, 0, 10), Vector3.up * 4, Color.red);
			});
		}
	}
	
	public struct DefaultCamera : IComponentData
	{}

	[UpdateBefore(typeof(TickClientPresentationSystem))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	[AlwaysUpdateSystem]
	public class ManageClientCameraSystem : GameBaseSystem
	{
		private Camera      m_Camera;
		private EntityQuery m_GameCameraQuery;

		public Camera Current => m_Camera;

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

			var clientWorldCount = 0;
			foreach (var world in World.AllWorlds)
				if (world.GetExistingSystem<ClientPresentationSystemGroup>() != null)
				{
					var presentationSystemGroup = world.GetExistingSystem<ClientPresentationSystemGroup>();
					var cameraSystem            = world.GetExistingSystem<ClientCreateCameraSystem>();
					if (cameraSystem != null)
						cameraSystem.InternalSetActive(presentationSystemGroup.Enabled);

					clientWorldCount++;
				}

			if (clientWorldCount == 0 && m_Camera != null)
				m_Camera.gameObject.SetActive(true);
			else if (clientWorldCount > 0) m_Camera.gameObject.SetActive(false);
		}
	}
}