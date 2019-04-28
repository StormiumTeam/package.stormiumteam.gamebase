using System;
using StormiumShared.Core.Networking;
using Unity.Entities;

namespace StormiumTeam.GameBase.Components
{
	// not used anymore, todo: remove it
	/*[Serializable]
	public struct CreateHealthInstance
	{
		public Entity asset;
		public Entity owner;
	}

	public class HealthInstanceProvider : SystemProvider<CreateHealthInstance>
	{
		public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedStreamerComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<HealthDescription>()
			};
			excludedStreamerComponents = null;
		}

		protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
		{
			return EntityManager.CreateEntity(typeof(HealthDescription));
		}

		protected override void DestroyEntity(Entity worldEntity)
		{

		}

		public override Entity SpawnLocalEntityWithArguments(CreateHealthInstance data)
		{
			return SpawnLocal(data.asset, data.owner);
		}

		public Entity SpawnLocal(Entity asset, Entity owner)
		{
			var copy = EntityManager.Instantiate(asset);

			EntityManager.RemoveComponent<AssetDescription>(copy);
			EntityManager.RemoveComponent<HealthAssetDescription>(copy);
			EntityManager.AddComponent(copy, typeof(HealthDescription));

			EntityManager.ReplaceOwnerData(copy, owner);

			return copy;
		}
	}*/
}