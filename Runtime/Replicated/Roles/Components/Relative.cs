using Unity.Entities;

namespace GameBase.Roles.Components
{
	/// <summary>
	/// A relative path to a <see cref="Entity"/> with <see cref="Interfaces.IEntityDescription"/>
	/// </summary>
	/// <typeparam name="TDescription"></typeparam>
	public readonly struct Relative<TDescription> : IComponentData
		where TDescription : Interfaces.IEntityDescription
	{
		/// <summary>
		/// Path to the entity
		/// </summary>
		public readonly Entity Target;

		public Relative(Entity target)
		{
			Target = target;
		}
	}
}