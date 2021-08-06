using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Interfaces;
using Unity.Entities;

namespace Utility.DOTS
{
	public static class EntityOwnerExtensions
	{
		public static void ReplaceOwnerData<T>(this EntityManager entityManager, Entity entity, Entity owner, bool autoEntityLink = true)
			where T : IEntityDescription
		{
			ReplaceOwnerData(entityManager, entity, owner, autoEntityLink);
			entityManager.AddComponentData(entity, new Relative<T>(owner));
		}
		
		public static void ReplaceOwnerData(this EntityManager entityManager, Entity entity, Entity owner, bool autoEntityLink = true)
		{
			entityManager.SetOrAddComponentData(entity, new Owner(owner));

			if (autoEntityLink)
			{
				// If the entity had an owner before, delete it from the old linked group
				if (entityManager.HasComponent(entity, typeof(Owner)))
				{
					var previousOwner = entityManager.GetComponentData<Owner>(entity);
					if (entityManager.HasComponent(previousOwner.Target, typeof(LinkedEntityGroup)))
					{
						var previousLinkedEntityGroup = entityManager.GetBuffer<LinkedEntityGroup>(previousOwner.Target);
						for (var i = 0; i != previousLinkedEntityGroup.Length; i++)
							if (previousLinkedEntityGroup[i].Value == entity)
							{
								previousLinkedEntityGroup.RemoveAt(i);
								i--;
							}
					}
				}

				if (!entityManager.HasComponent(owner, typeof(LinkedEntityGroup)))
				{
					// todo: check in future if LinkedEntityGroup behavior change!
					entityManager.AddBuffer<LinkedEntityGroup>(owner)
					             .Add(new LinkedEntityGroup {Value = owner});
				}

				var linkedEntityGroup = entityManager.GetBuffer<LinkedEntityGroup>(owner);
				linkedEntityGroup.Add(entity);
			}
		}
	}
}