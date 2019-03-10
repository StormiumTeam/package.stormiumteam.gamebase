using Unity.Entities;

namespace Stormium.Core
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