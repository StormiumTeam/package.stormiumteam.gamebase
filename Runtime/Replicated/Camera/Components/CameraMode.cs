using GameHost;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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

	public interface ICameraStateHolder
	{
		public ref CameraState Data { get; }
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

	public struct LocalCameraState : ICameraStateHolder, IComponentData
	{
		[SerializeField]
		private CameraState data;

		public unsafe ref CameraState Data
		{
			get
			{
				fixed (LocalCameraState* state = &this) return ref state->data;
			}
		}

		public CameraMode     Mode   => Data.Mode;
		public Entity         Target => Data.Target;
		public RigidTransform Offset => Data.Offset;

		public class Register : RegisterGameHostComponentData<LocalCameraState>
		{
			public override    string                       ComponentPath      => "StormiumTeam.GameBase.Camera.Components::LocalCameraState";
			protected override ICustomComponentDeserializer CustomDeserializer => new CustomSingleDeserializer<LocalCameraState, CameraStateDeserializer<LocalCameraState>>();
		}
	}

	public struct ServerCameraState : ICameraStateHolder, IComponentData
	{
		[SerializeField]
		private CameraState data;

		public unsafe ref CameraState Data
		{
			get
			{
				fixed (ServerCameraState* state = &this) return ref state->data;
			}
		}

		public CameraMode     Mode   => Data.Mode;
		public Entity         Target => Data.Target;
		public RigidTransform Offset => Data.Offset;

		public class Register : RegisterGameHostComponentData<ServerCameraState>
		{
			public override    string                       ComponentPath      => "StormiumTeam.GameBase.Camera.Components::ServerCameraState";
			protected override ICustomComponentDeserializer CustomDeserializer => new CustomSingleDeserializer<ServerCameraState, CameraStateDeserializer<ServerCameraState>>();
		}
	}

	public unsafe struct CameraStateDeserializer<T> : IValueDeserializer<T>
		where T : struct, ICameraStateHolder, IComponentData
	{
		// RigidTransform size is the same as the BEPU one, but Position and Rotation are reversed (bepu, position first, unity, rotation first)
		public int Size => sizeof(CameraMode) + sizeof(GhGameEntitySafe) + sizeof(RigidTransform);

		public void Deserialize(EntityManager em, NativeHashMap<GhGameEntitySafe, Entity> ghEntityToUEntity, ref T component, ref DataBufferReader reader)
		{
			component.Data.Mode = reader.ReadValue<CameraMode>();
			ghEntityToUEntity.TryGetValue(reader.ReadValue<GhGameEntitySafe>(), out component.Data.Target);
			component.Data.Offset.pos = reader.ReadValue<float3>();
			component.Data.Offset.rot = reader.ReadValue<quaternion>();
		}
	}
}