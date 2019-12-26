using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase.Components
{
	public struct TargetDamageEvent : IReadWriteComponentSnapshot<TargetDamageEvent, GhostSetup>
	{
		public Entity Origin;
		public Entity Destination;
		public int    Damage;

		[UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities.SpawnEvent))]
		public class Provider : BaseProviderBatch<TargetDamageEvent>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(GameEvent),
					typeof(TargetDamageEvent),
					typeof(GhostEntity)
				};
			}

			public override void SetEntityData(Entity entity, TargetDamageEvent data)
			{
				EntityManager.SetComponentData(entity, data);
			}

			protected override void OnUpdate()
			{
				EntityManager.DestroyEntity(Entities.WithAll<GameEvent, TargetDamageEvent>().ToEntityQuery());

				base.OnUpdate();
			}
		}

		[UpdateInGroup(typeof(AfterSnapshotIsAppliedSystemGroup))]
		public class SetManualEvent : ComponentSystem
		{
			protected override void OnUpdate()
			{
				Entities.WithAll<TargetDamageEvent>()
				        .WithNone<ManualDestroy>()
				        .ForEach(ent =>
				        {
					        EntityManager.AddComponent(ent, typeof(ManualDestroy));
					        EntityManager.AddComponent(ent, typeof(GameEvent));
				        });
			}
		}

		public void WriteTo(DataStreamWriter writer, ref TargetDamageEvent baseline, GhostSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUInt(setup[Origin], jobData.NetworkCompressionModel);
			writer.WritePackedUInt(setup[Destination], jobData.NetworkCompressionModel);
			writer.WritePackedInt(Damage, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref TargetDamageEvent baseline, DeserializeClientData jobData)
		{
			jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel), out Origin);
			jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel), out Destination);
			Damage = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
		}

		public class Sync : MixedComponentSnapshotSystem<TargetDamageEvent, GhostSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public struct Exclude : IComponentData
		{
		}
	}
}