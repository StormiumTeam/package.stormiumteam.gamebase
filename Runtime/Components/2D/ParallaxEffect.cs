using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase._2D
{
	public class ParallaxEffect : MonoBehaviour, IConvertGameObjectToEntity
	{
		public float multiplier;
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentObject(entity, this);
		}
	}
}