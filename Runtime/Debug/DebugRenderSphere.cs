using System;
using Unity.Entities;
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
	public struct DebugRenderSphereShared : ISharedComponentData, IEquatable<DebugRenderSphereShared>
	{
		public Color color;

		public Mesh     mesh;
		public Material material;
		public int      subMesh;

		[LayerField]
		public int layer;

		public ShadowCastingMode castShadows;
		public bool              receiveShadows;

		public bool Equals(DebugRenderSphereShared other)
		{
			return color.Equals(other.color)
			       && Equals(mesh, other.mesh)
			       && Equals(material, other.material)
			       && subMesh == other.subMesh
			       && layer == other.layer
			       && castShadows == other.castShadows
			       && receiveShadows == other.receiveShadows;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is DebugRenderSphereShared other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = color.GetHashCode();
				hashCode = (hashCode * 397) ^ (mesh != null ? mesh.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (material != null ? material.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ subMesh;
				hashCode = (hashCode * 397) ^ layer;
				hashCode = (hashCode * 397) ^ (int) castShadows;
				hashCode = (hashCode * 397) ^ receiveShadows.GetHashCode();
				return hashCode;
			}
		}
	}

	//[UpdateBefore(typeof(RenderMeshSystemV2))]
	public class DebugRenderSphereSystem : ComponentSystem
	{
		private static readonly int         BaseColor = Shader.PropertyToID("_BaseColor");
		private                 EntityQuery m_FindQuery;
		private                 Material    m_Material;
		private                 Mesh        m_SphereMesh;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_FindQuery = GetEntityQuery(typeof(DebugRenderSphere), typeof(LocalToWorld), ComponentType.Exclude<DebugRenderSphereShared>());

			var tempGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			m_SphereMesh = Object.Instantiate(tempGo.GetComponent<MeshFilter>().mesh);
			m_Material   = Object.Instantiate(tempGo.GetComponent<Renderer>().sharedMaterial);

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