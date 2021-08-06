using GameHost;
using GameHost.ShareSimuWorldFeature;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Interfaces;
using Unity.Collections;
using Unity.Entities;

namespace StormiumTeam.GameBase.Systems.Roles
{
	[UpdateInGroup(typeof(ReceiveLastFrameGhSimulationSystemGroup), OrderFirst = true)]
	public class BuildContainerSystem<TDescription> : AbsGameBaseSystem
		where TDescription : IEntityDescription
	{
		private EntityQuery ownerQuery, childQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			ownerQuery = GetEntityQuery(typeof(OwnedRelative<TDescription>));
			childQuery = GetEntityQuery(typeof(TDescription), typeof(Owner));
		}

		protected override void OnUpdate()
		{
			foreach (var owner in ownerQuery.ToEntityArray(Allocator.Temp))
			{
				var buffer = GetBuffer<OwnedRelative<TDescription>>(owner);
				if (buffer.Capacity == 1)
					buffer.ResizeUninitialized(buffer.Capacity + 1);

				buffer.Clear();
			}

			foreach (var child in childQuery.ToEntityArray(Allocator.Temp))
			{
				var owner = GetComponent<Owner>(child).Target;
				if (!EntityManager.TryGetBuffer<OwnedRelative<TDescription>>(owner, out var buffer))
					continue;

				buffer.Add(new OwnedRelative<TDescription>(child));
			}
		}
	}
}