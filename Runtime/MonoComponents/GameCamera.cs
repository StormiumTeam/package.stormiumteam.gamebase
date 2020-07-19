using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Components
{
	[RequireComponent(typeof(Camera))]
	public class GameCamera : MonoBehaviour, IConvertGameObjectToEntity
	{
		public Camera Camera { get; private set; }

		private void OnEnable()
		{
			Camera = GetComponent<Camera>();
		}

		private void OnDisable()
		{
			Camera = null;
		}

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
		}
	}
}