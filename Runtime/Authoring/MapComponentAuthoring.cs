using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Authoring
{
	public class MapComponentAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			conversionSystem.DeclareLinkedEntityGroup(gameObject);
			foreach (var tr in GetComponentsInChildren<Transform>())
			{
				if (tr.gameObject == gameObject)
					continue;
			}

			dstManager.AddComponent(entity, typeof(MapComponent));
		}
	}
	
	public struct MapComponent : IComponentData
	{
		
	}
}