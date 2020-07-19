using Unity.Entities;
using Unity.Mathematics;

namespace StormiumTeam.GameBase.Data
{
	public struct CameraModifierData : IComponentData
	{
		public float3     Position;
		public quaternion Rotation;
		public float      FieldOfView;
	}
}