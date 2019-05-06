using System;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace StormiumTeam.GameBase
{
	public struct DebugRenderSphere : IComponentData
	{
		public bool CastShadows;
		public bool ReceiveShadows;

		public Color Color;
	}

	[Serializable]
	public struct DebugRenderSphereShared : ISharedComponentData
	{
		public Color color;
		
		public Mesh     mesh;
		public Material material;
		public int      subMesh;
		[LayerField]
		public int layer;
		public ShadowCastingMode castShadows;
		public bool              receiveShadows;
	}

	[UpdateBefore(typeof(RenderMeshSystemV2))]
	public class DebugRenderSphereSystem : ComponentSystem
	{
		private EntityQuery m_FindQuery;
		private Mesh        m_SphereMesh;
		private Material m_Material;

		private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			m_FindQuery = GetEntityQuery(typeof(DebugRenderSphere), typeof(LocalToWorld), ComponentType.Exclude<DebugRenderSphereShared>());

			var tempGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			m_SphereMesh = Object.Instantiate(tempGo.GetComponent<MeshFilter>().mesh);
			m_Material = Object.Instantiate(tempGo.GetComponent<Renderer>().sharedMaterial);

			Object.Destroy(tempGo);
		}

		protected override void OnUpdate()
		{
			Entities.With(m_FindQuery).ForEach((Entity e, ref DebugRenderSphere debugRender) =>
			{
				var material = Object.Instantiate(m_Material);
				material.SetColor(BaseColor, debugRender.Color);
				material.enableInstancing = true;

				PostUpdateCommands.AddSharedComponent(e, new DebugRenderSphereShared
				{
					castShadows    = debugRender.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
					layer          = 0,
					material       = material,
					mesh           = m_SphereMesh,
					color          = debugRender.Color,
					receiveShadows = debugRender.ReceiveShadows
				});
			});

			Entities.ForEach((Entity e, DebugRenderSphereShared renderMesh, ref LocalToWorld tr, ref DebugRenderSphere debugRender) =>
			{
				if (renderMesh.color != debugRender.Color)
				{
					renderMesh.color = debugRender.Color;
					renderMesh.material.SetColor(BaseColor, debugRender.Color);
					
					PostUpdateCommands.SetSharedComponent(e, renderMesh);
				}

				Graphics.DrawMesh(renderMesh.mesh, tr.Value, renderMesh.material, renderMesh.layer, Camera.main, 0, null, true, true);
			});
		}
	}
}