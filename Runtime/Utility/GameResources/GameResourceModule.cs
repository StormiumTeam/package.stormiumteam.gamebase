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
	public class GameResourceModule<TResource> : BaseSystemModule
		where TResource : struct, IEquatable<TResource>, IGameResourceDescription
	{
		public override ModuleUpdateType UpdateType => ModuleUpdateType.MainThread;

		private EntityQuery resourceQuery;
		private double      lastUpdateFrame;
		
		private NativeHashMap<TResource, GameResource<TResource>> keyMap;

		protected override void OnEnable()
		{
			resourceQuery = EntityManager.CreateEntityQuery(typeof(IsResourceEntity), typeof(TResource), typeof(ReplicatedGameEntity));
			keyMap        = new NativeHashMap<TResource, GameResource<TResource>>(32, Allocator.Persistent);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
			lastUpdateFrame = System.Time.ElapsedTime;
			
			keyMap.Clear();
			
			using var keyArray        = resourceQuery.ToComponentDataArray<TResource>(Allocator.Temp);
			using var replicatedArray = resourceQuery.ToComponentDataArray<ReplicatedGameEntity>(Allocator.Temp);
			for (var i = 0; i != keyArray.Length; i++)
			{
				keyMap[keyArray[i]] = new GameResource<TResource>(replicatedArray[i].Source);
			}
		}

		protected override void OnDisable()
		{
			resourceQuery.Dispose();
			keyMap.Dispose();
		}

		public GameResource<TResource> GetResourceOrDefault(TResource key)
		{
			if (!lastUpdateFrame.Equals(System.Time.ElapsedTime))
				Update();

			keyMap.TryGetValue(key, out var resource);
			return resource;
		}

		public (GameResource<TResource>, TResource) GetResourceTuple(TResource key)
		{
			if (!lastUpdateFrame.Equals(System.Time.ElapsedTime))
				Update();

			keyMap.TryGetValue(key, out var resource);
			return (resource, key);
		}
	}
}