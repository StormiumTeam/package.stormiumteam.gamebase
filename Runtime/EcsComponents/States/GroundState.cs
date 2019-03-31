using Unity.Entities;

namespace StormiumTeam.GameBase
{
	public struct GroundState : IComponentData
	{
		public bool Value;

		public GroundState(bool isGrounded)
		{
			Value = isGrounded;
		}
	}
}