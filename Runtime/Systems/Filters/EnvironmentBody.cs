using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Filters
{
	public struct EnvironmentTag : IComponentData
	{
	}

	public class EnvironmentBody : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new EnvironmentTag());
		}
	}
}