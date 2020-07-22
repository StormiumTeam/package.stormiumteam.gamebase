using Unity.Entities;

namespace StormiumTeam.GameBase.Utility.DOTS.xCamera
{
	/// <summary>
	/// Useful for a CameraState that need to modify another camera than the default one.
	/// </summary>
	public struct CameraStateCameraTarget : IComponentData
	{
		public Entity Value;
	}
}