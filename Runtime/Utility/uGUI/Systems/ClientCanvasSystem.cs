using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace StormiumTeam.GameBase.Utility.uGUI.Systems
{
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class ClientCanvasSystem : ComponentSystem
	{
		private string       hack0;
		private int          hack1;
		public  List<Canvas> Canvas { get; private set; }

		public Canvas Current { get; private set; }

		protected override void OnCreate()
		{
			base.OnCreate();

			Canvas = new List<Canvas>();

			var gameObject = new GameObject($"(World: {World.Name}) UICanvas",
				typeof(Canvas),
				typeof(CanvasScaler),
				typeof(GraphicRaycaster));

			Current            = gameObject.GetComponent<Canvas>();
			Current.renderMode = RenderMode.ScreenSpaceOverlay;

			var canvasScaler = gameObject.GetComponent<CanvasScaler>();
			canvasScaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution    = new Vector2(1920, 1080);
			canvasScaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			canvasScaler.matchWidthOrHeight     = 0;
			canvasScaler.referencePixelsPerUnit = 100;

			var graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
			graphicRaycaster.ignoreReversedGraphics = true;
			graphicRaycaster.blockingObjects        = GraphicRaycaster.BlockingObjects.None;
		}

		protected override void OnUpdate()
		{
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			foreach (var canvas in Canvas)
				if (canvas != null)
					Object.Destroy(canvas.gameObject);

			Canvas.Clear();

			if (Current != null)
				Object.Destroy(Current.gameObject);

			Current = null;
			Canvas  = null;
		}

		public Canvas CreateCanvas(out int listIndex, string name = "UICustomCanvas", bool defaultInitialization = false, bool defaultAddRaycaster = true)
		{
			var gameObject = new GameObject($"(World: {World.Name}) {name}#{Canvas.Count}",
				typeof(Canvas),
				typeof(CanvasScaler),
				typeof(GraphicRaycaster));
			var canvas = gameObject.GetComponent<Canvas>();
			hack1 = canvas.sortingOrder;
			hack0 = canvas.sortingLayerName;

			listIndex = Canvas.Count;
			if (!defaultInitialization)
				return canvas;

			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			var canvasScaler = gameObject.GetComponent<CanvasScaler>();
			canvasScaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution    = new Vector2(1920, 1080);
			canvasScaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			canvasScaler.matchWidthOrHeight     = 0;
			canvasScaler.referencePixelsPerUnit = 100;

			if (defaultAddRaycaster)
			{
				var graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
				graphicRaycaster.ignoreReversedGraphics = true;
				graphicRaycaster.blockingObjects        = GraphicRaycaster.BlockingObjects.None;
			}

			return canvas;
		}
	}
}