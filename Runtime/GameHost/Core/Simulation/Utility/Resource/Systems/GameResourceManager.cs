using System;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Utility.Resource.Components;
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

		public bool TryGetResource<TResource, TKey>(GameResource<TResource> resource, out TKey key)
			where TResource : IGameResourceDescription
			where TKey : struct, IEquatable<TKey>, IGameResourceKeyDescription
		{
			if (TryGetRawEntity(resource, out var entity)
			    && EntityManager.TryGetComponentData(entity, out GameResourceKey<TKey> resourceKey))
			{
				key = resourceKey.Value;
				return true;
			}

			key = default;
			return false;
		}
	}

	public static class GameResourceManagerExtensions
	{
		public static bool TryGet<TResource, TKey>(this GameResource<TResource> resource, GameResourceManager mgr, out TKey key)
			where TResource : IGameResourceDescription
			where TKey : struct, IEquatable<TKey>, IGameResourceKeyDescription
		{
			return mgr.TryGetResource(resource, out key);
		}
	}
}