using System.Collections;
using StormiumTeam.GameBase.Bootstrapping;
using StormiumTeam.GameBase.Systems;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Authoring
{
	public class BootstrapAuthoring : MonoBehaviour
	{
		public string   Name;
		public string[] Parameters;

		private IEnumerator Start()
		{
			while (!GameLoader.Loaded)
				yield return null;

			// Make sure that the systems are properly initialized
			yield return null;

			var world = World.DefaultGameObjectInjectionWorld;
			Debug.LogWarning($"Injecting to {world.Name}");
			
			Convert(world.EntityManager.CreateEntity(), world.EntityManager, null);
			Destroy(gameObject);
		}

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			var bootstrapQuery    = dstManager.CreateEntityQuery(typeof(BootstrapComponent));
			var bootstrapEntities = bootstrapQuery.ToEntityArray(Allocator.TempJob);
			var targetBootstrap   = default(Entity);
			foreach (var e in bootstrapEntities)
			{
				var bootstrap = dstManager.GetComponentData<BootstrapComponent>(e);
				Debug.LogError(dstManager.World.Name + "> " + bootstrap.Name);
				if (bootstrap.Name == Name)
				{
					targetBootstrap = e;
					break;
				}
			}

			bootstrapEntities.Dispose();

			if (targetBootstrap != default)
			{
				dstManager.AddSharedComponentData(entity, new TargetBootstrap
				{
					Value = targetBootstrap
				});
				dstManager.AddComponentData(entity, new BootstrapParameters
				{
					Values = Parameters ?? new string[0]
				});
			}
			else
				Debug.LogError("No Bootstrap found with name: " + Name);
		}
	}
}