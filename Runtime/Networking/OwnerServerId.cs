using package.stormiumteam.shared.ecs;
using Revolution;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase
{
	public struct OwnerServerId : IReadWriteComponentSnapshot<OwnerServerId>, ISnapshotDelta<OwnerServerId>
	{
		public int Value;

		public void WriteTo(DataStreamWriter writer, ref OwnerServerId baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedInt(Value, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref OwnerServerId baseline, DeserializeClientData jobData)
		{
			Value = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
		}

		public bool DidChange(OwnerServerId baseline)
		{
			return Value != baseline.Value;
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronization : MixedComponentSnapshotSystemDelta<OwnerServerId>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}

	public struct HasAuthorityFromServer : IComponentData
	{
	}

	[UpdateInGroup(typeof(AfterSnapshotIsAppliedSystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class ApplyOwnerServerIdSystem : AbsGameBaseSystem
	{
		private LazySystem<EndSimulationEntityCommandBufferSystem> m_EndBarrier;

		protected override void OnCreate()
		{
			base.OnCreate();

			RequireSingletonForUpdate<NetworkIdComponent>();
		}

		protected override void OnUpdate()
		{
			var currentId           = GetSingleton<NetworkIdComponent>().Value;
			var ecb                 = this.L(ref m_EndBarrier).CreateCommandBuffer().ToConcurrent();
			var authorityFromEntity = GetComponentDataFromEntity<HasAuthorityFromServer>(true);

			Entities.ForEach((Entity entity, int nativeThreadIndex, in OwnerServerId ownerId) =>
			{
				var existence = authorityFromEntity.Exists(entity);
				if (ownerId.Value == currentId && !existence)
					ecb.AddComponent(nativeThreadIndex, entity, new HasAuthorityFromServer());
				else if (ownerId.Value != currentId && existence)
					ecb.RemoveComponent<HasAuthorityFromServer>(nativeThreadIndex, entity);
			}).WithReadOnly(authorityFromEntity).Schedule();

			m_EndBarrier.Value.AddJobHandleForProducer(Dependency);
		}
	}
}