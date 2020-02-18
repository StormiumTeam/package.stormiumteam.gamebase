using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<BuffModifierDescription>))]

namespace StormiumTeam.GameBase.Components
{
	public struct BuffModifierDescription : IEntityDescription
	{
		public class Sync : RelativeSynchronize<BuffModifierDescription>
		{
		}
	}

	public struct BuffSource : IComponentData
	{
		public Entity Source;
	}

	public struct BuffForTarget : IComponentData
	{
		public Entity Target;
	}

	[InternalBufferCapacity(1)]
	public struct BuffContainer : IBufferElementData
	{
		public Entity Target;

		public BuffContainer(Entity t)
		{
			Target = t;
		}
	}
}