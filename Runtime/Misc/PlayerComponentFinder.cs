using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;

namespace Misc
{
	// I'm not a big fan on how this class is present.
	public static class PlayerComponentFinder
	{
		public static Entity FindPlayerComponent(EntityQuery findQuery, Entity player)
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

		public static Entity GetComponentFromPlayer<TDescription>(EntityManager entityManager, EntityQuery findQuery, Entity target, Entity fallback = default)
			where TDescription : struct, IEntityDescription
		{
			if (entityManager.TryGetComponentData(target, out Relative<TDescription> relativeRhythmEngine)
			    && relativeRhythmEngine.Target != default)
				return relativeRhythmEngine.Target;

			if (entityManager.TryGetComponentData(target, out Relative<PlayerDescription> relativePlayer)
			    && relativePlayer.Target != default)
				return FindPlayerComponent(findQuery, relativePlayer.Target);

			return FindPlayerComponent(findQuery, fallback);
		}
	}
}