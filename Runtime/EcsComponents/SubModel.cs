using Unity.Entities;

namespace StormiumTeam.GameBase
{
	public struct SubModel : IComponentData
	{
		public Entity Target;

		public SubModel(Entity entity)
		{
			Target = entity;
		}
	}
}