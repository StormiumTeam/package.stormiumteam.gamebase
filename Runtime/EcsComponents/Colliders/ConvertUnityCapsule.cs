using System.Collections;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Components
{
	[ExecuteAlways]
	public class ConvertUnityCapsule : MonoBehaviour, IConvertGameObjectToEntity
	{
		public bool destroyOnConvert = true;
		
		protected void OnEnable()
		{
			var gameObjectEntity = GetComponent<GameObjectEntity>();
			if (gameObjectEntity == null)
				return;

			if (gameObjectEntity.Entity == default)
			{
				gameObjectEntity.enabled = false;
				gameObjectEntity.enabled = true;
			}

			AddThis(gameObjectEntity.Entity, gameObjectEntity.EntityManager);
		}

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			AddThis(entity, dstManager);
		}

		private void AddThis(Entity entity, EntityManager entityManager)
		{
			Debug.Log("Adding thins to " + entity);
			
			if (!entityManager.HasComponent<ConvertUnityCapsule>(entity))
				entityManager.AddComponentObject(entity, this);
			if (!entityManager.HasComponent<CapsuleShapeData>(entity))
				entityManager.AddComponent(entity, typeof(CapsuleShapeData));
		}
	}
}