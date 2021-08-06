using GameHost;
using StormiumTeam.GameBase.Roles.Interfaces;
using Unity.Entities;

namespace StormiumTeam.GameBase.Roles.Components
{
	// The reason why OwnedRelative is not replicated is that the client can fill it automatically
	[InternalBufferCapacity(1)]
	public struct OwnedRelative<T> : IBufferElementData
		where T : IEntityDescription
	{
		public readonly Entity Target;

		public OwnedRelative(Entity target) => Target = target;
	}
}