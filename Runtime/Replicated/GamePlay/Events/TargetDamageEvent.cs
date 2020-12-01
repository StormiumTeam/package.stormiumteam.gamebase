using GameHost;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StormiumTeam.GameBase.GamePlay.Events
{
	public struct TargetDamageEvent : IComponentData, IValueDeserializer<TargetDamageEvent>
	{
		public struct Replicated
		{
			public GhGameEntitySafe Instigator;
			public GhGameEntitySafe Victim;

			public double Damage;
		}

		public Entity Instigator;
		public Entity Victim;

		public double Damage;

		public class Register : RegisterGameHostComponentData<TargetDamageEvent>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new CustomSingleDeserializer<TargetDamageEvent, TargetDamageEvent>();
		}

		public int Size => UnsafeUtility.SizeOf<Replicated>();

		public void Deserialize(EntityManager em, NativeHashMap<GhGameEntitySafe, Entity> ghEntityToUEntity, ref TargetDamageEvent component, ref DataBufferReader reader)
		{
			var replicated = reader.ReadValue<Replicated>();

			Damage = replicated.Damage;
			ghEntityToUEntity.TryGetValue(replicated.Instigator, out Instigator);
			ghEntityToUEntity.TryGetValue(replicated.Victim, out Victim);

			component = this;
		}
	}
}