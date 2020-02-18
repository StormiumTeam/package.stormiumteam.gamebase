using Revolution;
using Revolution.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace Stormium.Core.Projectiles
{
	public struct ProjectileEndedTag : IComponentData
	{
		public class Sync : ComponentSnapshotSystemTag<ProjectileEndedTag> {}
	}
	
	public struct ProjectileExplodedEndReason : IReadWriteComponentSnapshot<ProjectileExplodedEndReason>
	{
		public float3 normal;
		
		public class Sync : MixedComponentSnapshotSystem<ProjectileExplodedEndReason, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(ExcludeFromTagging);
		}

		public void WriteTo(DataStreamWriter writer, ref ProjectileExplodedEndReason baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			var quantized = new QuantizedFloat3();
			quantized.Set(100, normal);

			for (var i = 0; i != 3; i++) writer.WritePackedInt(quantized[i], jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref ProjectileExplodedEndReason baseline, DeserializeClientData jobData)
		{
			var quantized = new QuantizedFloat3();
			for (var i = 0; i != 3; i++) quantized[i] = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);

			normal = quantized.Get(0.01f);
		}
	}

	public struct ProjectileOutOfTimeEndReason : IComponentData
	{
		public class Sync : ComponentSnapshotSystemTag<ProjectileOutOfTimeEndReason> {}
	}
}