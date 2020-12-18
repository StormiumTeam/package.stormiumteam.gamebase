using System;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Utility.Resource.Interfaces;
using package.stormiumteam.shared.ecs;
using Unity.Entities;

namespace GameHost.Simulation.Utility.Resource.Systems
{
	public class GameResourceManager : SystemBase
	{
		private ReceiveSimulationWorldSystem receiveSystem;

		protected override void OnCreate()
		{
			base.OnCreate();
			Enabled = false;

			receiveSystem = World.GetExistingSystem<ReceiveSimulationWorldSystem>();
		}

		protected override void OnUpdate()
		{
		}

		public bool TryGetRawEntity<TResource>(GameResource<TResource> resource, out Entity entity)
			where TResource : IGameResourceDescription
		{
			return receiveSystem.ghToUnityEntityMap.TryGetValue(resource.Entity, out entity);
		}

		public bool TryGetResource<TResource>(GameResource<TResource> resource, out TResource res)
			where TResource : struct, IEquatable<TResource>, IGameResourceDescription
		{
			if (TryGetRawEntity(resource, out var entity)
			    && EntityManager.TryGetComponentData(entity, out TResource resourceKey))
			{
				res = resourceKey;
				return true;
			}

			res = default;
			return false;
		}
	}

	public static class GameResourceManagerExtensions
	{
		public static bool TryGet<TResource>(this GameResource<TResource> resource, GameResourceManager mgr, out TResource res)
			where TResource : struct, IEquatable<TResource>, IGameResourceDescription
		{
			return mgr.TryGetResource(resource, out res);
		}
	}
}