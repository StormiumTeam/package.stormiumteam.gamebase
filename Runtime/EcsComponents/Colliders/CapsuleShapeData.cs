using System;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StormiumTeam.GameBase.Data
{
	[Serializable]
	public struct CapsuleShapeData : IComponentData
	{
		public float radius;
		public float height;

		public float3 offset;

		public float3 Center => height / 2 + offset;
	}

	[ExecuteAlways]
	public class ConvertUnityCapsuleSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((Entity entity, ref CapsuleShapeData capsuleShapeData, ConvertUnityCapsule convert, CapsuleCollider original) =>
			{
				capsuleShapeData.height = original.height;
				capsuleShapeData.radius = original.radius;
				capsuleShapeData.offset = original.center;

				if (!convert.destroyOnConvert) 
					return;
				
				Object.Destroy(convert);
				PostUpdateCommands.RemoveComponent(entity, typeof(ConvertUnityCapsule));
			});
		}
	}
}