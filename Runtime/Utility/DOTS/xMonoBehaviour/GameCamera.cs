using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Utility.DOTS.xMonoBehaviour
{
	[RequireComponent(typeof(Camera))]
	public class GameCamera : MonoBehaviour, IConvertGameObjectToEntity
	{
		public Camera Camera { get; private set; }

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
		}

		private void OnEnable()
		{
			Camera = GetComponent<Camera>();
		}

		private void OnDisable()
		{
			Camera = null;
		}
	}
}