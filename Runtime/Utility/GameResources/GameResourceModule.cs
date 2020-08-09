using System;
using GameHost;
using GameHost.Simulation.Utility.Resource;
using GameHost.Simulation.Utility.Resource.Components;
using GameHost.Simulation.Utility.Resource.Interfaces;
using StormiumTeam.GameBase.Utility.Modules;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Utility.GameResources
{
	public class GameResourceModule<TResource, TKey> : BaseSystemModule
		where TResource : IGameResourceDescription
		where TKey : struct, IGameResourceKeyDescription, IEquatable<TKey>
	{
		public override ModuleUpdateType UpdateType => ModuleUpdateType.MainThread;

		private EntityQuery resourceQuery;
		private double      lastUpdateFrame;
		
		private NativeHashMap<TKey, GameResource<TResource>> keyMap;

		protected override void OnEnable()
		{
			resourceQuery = EntityManager.CreateEntityQuery(typeof(IsResourceEntity), typeof(GameResourceKey<TKey>), typeof(ReplicatedGameEntity));
			keyMap        = new NativeHashMap<TKey, GameResource<TResource>>(32, Allocator.Persistent);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
			lastUpdateFrame = System.Time.ElapsedTime;
			
			keyMap.Clear();

			using var keyArray        = resourceQuery.ToComponentDataArray<GameResourceKey<TKey>>(Allocator.Temp);
			using var replicatedArray = resourceQuery.ToComponentDataArray<ReplicatedGameEntity>(Allocator.Temp);
			for (var i = 0; i != keyArray.Length; i++)
			{
				keyMap[keyArray[i].Value] = new GameResource<TResource>(replicatedArray[i].Source);
			}
		}

		protected override void OnDisable()
		{
			resourceQuery.Dispose();
			keyMap.Dispose();
		}

		public GameResource<TResource> GetResourceOrDefault(TKey key)
		{
			if (!lastUpdateFrame.Equals(System.Time.ElapsedTime))
				Update();

			keyMap.TryGetValue(key, out var resource);
			return resource;
		}

		public (GameResource<TResource>, TKey) GetResourceTuple(TKey key)
		{
			if (!lastUpdateFrame.Equals(System.Time.ElapsedTime))
				Update();

			keyMap.TryGetValue(key, out var resource);
			return (resource, key);
		}
	}
}