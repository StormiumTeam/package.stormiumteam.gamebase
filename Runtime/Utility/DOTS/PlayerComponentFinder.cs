using GameBase.Roles.Components;
using GameBase.Roles.Descriptions;
using GameBase.Roles.Interfaces;
using package.stormiumteam.shared.ecs;
using Unity.Collections;
using Unity.Entities;

namespace StormiumTeam.GameBase.Utility.DOTS
{
	// I'm not a big fan on how this class is present.
	public static class PlayerComponentFinder
	{
		public static Entity FromQueryFindPlayerChild(EntityQuery findQuery, Entity player)
		{
			var entities        = findQuery.ToEntityArray(Allocator.TempJob);
			var relativePlayers = findQuery.ToComponentDataArray<Relative<PlayerDescription>>(Allocator.TempJob);
			for (var ent = 0; ent < entities.Length; ent++)
			{
				if (relativePlayers[ent].Target == player)
					return entities[ent];
			}

			entities.Dispose();
			relativePlayers.Dispose();

			return default;
		}

		public static Entity GetRelativeChild<TDescription>(EntityManager entityManager, EntityQuery findQuery, Entity target, Entity fallback = default)
			where TDescription : struct, IEntityDescription
		{
			if (entityManager.TryGetComponentData(target, out Relative<TDescription> relativeFromDescription)
			    && relativeFromDescription.Target != default)
				return relativeFromDescription.Target;

			if (entityManager.TryGetComponentData(target, out Relative<PlayerDescription> relativePlayer)
			    && relativePlayer.Target != default)
				return FromQueryFindPlayerChild(findQuery, relativePlayer.Target);

			return FromQueryFindPlayerChild(findQuery, fallback);
		}
	}
}