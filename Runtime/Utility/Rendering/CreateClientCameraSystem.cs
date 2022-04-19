using System;
using System.Linq;
using DefaultNamespace.Utility.DOTS;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Utility.DOTS.xMonoBehaviour;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace StormiumTeam.GameBase.Utility.Rendering
{
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	[AlwaysUpdateSystem]
	public class ClientCreateCameraSystem : AbsGameBaseSystem
	{
		private bool m_PreviousState;

		public Camera        Camera        { get; private set; }
		public Camera        UICamera      { get; private set; }
		public AudioListener AudioListener { get; private set; }

		public RenderTexture UIRenderTexture { get; private set; }
		public Canvas        UICanvas        { get; private set; }

		public ClientCreateCameraSystem()
		{
			static Camera createCamera(string name, params Type[] additionalComponents)
			{
				GameObject gameObject;
				gameObject = new GameObject($"(World: {World.DefaultGameObjectInjectionWorld.Name}) {name}", additionalComponents.Concat(new [] {typeof(Camera)}).ToArray());
				
				var camera                  = gameObject.GetComponent<Camera>();
				camera.orthographicSize = 10;
				camera.fieldOfView      = 60;
				camera.nearClipPlane    = 0.025f;

				gameObject.transform.position = new Vector3(0, 0, -100);
			
				gameObject.SetActive(false);

				return camera;
			}

			Camera   = createCamera("GameCamera", typeof(GameCamera), typeof(GameObjectEntity));

			UICamera = createCamera("UICamera");

			UICamera.cullingMask              = 0;
			UICamera.eventMask                = 0;
			UICamera.overrideSceneCullingMask = 0;
			UICamera.useOcclusionCulling      = false;

			UIRenderTexture = new RenderTexture(new RenderTextureDescriptor(1920, 1080, GraphicsFormat.R8G8B8A8_SRGB, 24)
			{
				sRGB        = true,
				msaaSamples = 2
			})
			{
				filterMode = FilterMode.Trilinear
			};
			UICanvas            = new GameObject("UICanvas", new[] {typeof(Canvas)}).GetComponent<Canvas>();
			UICanvas.renderMode = RenderMode.ScreenSpaceOverlay;

			var rawImage = new GameObject("RawImageObject", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
			rawImage.GetComponent<RawImage>().texture = UIRenderTexture;

			rawImage.transform.parent = UICanvas.transform;
			rawImage.transform.localScale = Vector3.one;

			var rt = rawImage.GetComponent<RectTransform>();
			rt.anchorMin        = new Vector2(0, 0);
			rt.anchorMax        = new Vector2(1, 1);
			rt.sizeDelta        = Vector2.zero;
			rt.anchoredPosition = Vector2.zero;
			rt.pivot            = new Vector2(0.5f, 0.5f);

			UICamera.orthographic = true;

			UICamera.targetTexture = UIRenderTexture;

			var listenerGo = new GameObject($"(World: {World.DefaultGameObjectInjectionWorld.Name}) AudioListener", typeof(AudioListener));
			AudioListener = listenerGo.GetComponent<AudioListener>();
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			
			InternalSetActive(true);
		}

		protected override void OnUpdate()
		{
			if (UIRenderTexture.width != Screen.width || UIRenderTexture.height != Screen.height)
			{
				UIRenderTexture.Release();
				UIRenderTexture.width  = Screen.width;
				UIRenderTexture.height = Screen.height;
				UIRenderTexture.Create();
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (Camera != null)
				Object.Destroy(Camera.gameObject);
			if (UICamera != null)
				Object.Destroy(UICamera.gameObject);
			
			Camera   = null;
			UICamera = null;
		}

		internal void InternalSetActive(bool state)
		{
			if (state == m_PreviousState)
				return;

			m_PreviousState = state;

			using (new SetTemporaryInjectionWorld(World))
			{
				Camera.gameObject.SetActive(state);
				UICamera.gameObject.SetActive(state);
				UICamera.gameObject.SetActive(false);
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
				if (math.abs(camera.Camera.orthographicSize) < 0.1f)
					camera.Camera.orthographicSize = 0.25f;
			});

			var cam = World.GetExistingSystem<ClientCreateCameraSystem>();
			cam.AudioListener.transform.position = new Vector3
			{
				x = cam.Camera.transform.position.x,
				y = 0,
				z = 0
			};
		}
	}

	public struct DefaultCamera : IComponentData
	{
	}
}