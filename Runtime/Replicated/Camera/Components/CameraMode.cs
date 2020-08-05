using Unity.Entities;
using Unity.Mathematics;

namespace StormiumTeam.GameBase._Camera
{
	public enum CameraMode
	{
		/// <summary>
		///     The camera will not be ruled by this state and will revert to Default mode if there are
		///     no other states with '<see cref="Forced" />' mode.
		/// </summary>
		Default = 0,

		/// <summary>
		///     The camera will be forced to the rules of this state and override previous states.
		/// </summary>
		Forced = 1
	}

	public struct CameraState
	{
		public CameraMode Mode;

		public Entity         Target;
		public RigidTransform Offset;
	}

	public struct ComputedCameraState : IComponentData
	{
		public bool UseModifier;

		/// <summary>
		///     Entity from final camera state
		/// </summary>
		public Entity StateEntity;

		/// <summary>
		///     Camera state data
		/// </summary>
		public CameraState StateData;

		/// <summary>
		///     Field Of View.
		/// </summary>
		public float Focus;
	}

	public struct LocalCameraState : IComponentData
	{
		public CameraState Data;

		public CameraMode     Mode   => Data.Mode;
		public Entity         Target => Data.Target;
		public RigidTransform Offset => Data.Offset;
	}

	public struct ServerCameraState : IComponentData
	{
		public CameraState Data;

		public CameraMode     Mode   => Data.Mode;
		public Entity         Target => Data.Target;
		public RigidTransform Offset => Data.Offset;
	}
}