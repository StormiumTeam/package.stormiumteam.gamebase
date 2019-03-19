using Unity.Entities;

namespace StormiumTeam.GameBase
{
	public struct Orientation1D : IComponentData
	{
		public byte Value;

		public bool IsLeft => Value == 0;
		public bool IsValue => Value == 1;
	}
}