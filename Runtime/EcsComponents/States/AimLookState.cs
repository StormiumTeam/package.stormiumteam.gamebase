using Revolution;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase
{
	public struct AimLookState : IComponentData
	{
		public struct Exclude : IComponentData
		{
		}

		public float2 Aim;

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISynchronizeImpl<AimLookState>
		{
			public uint Tick { get; set; }

			public int2 Aim;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedIntDelta(Aim.x, baseline.Aim.x, compressionModel);
				writer.WritePackedIntDelta(Aim.y, baseline.Aim.y, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				Aim.x = reader.ReadPackedIntDelta(ref ctx, baseline.Aim.x, compressionModel);
				Aim.y = reader.ReadPackedIntDelta(ref ctx, baseline.Aim.y, compressionModel);
			}

			public void SynchronizeFrom(in AimLookState component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				Aim = (int2) (component.Aim * 1000);
			}

			public void SynchronizeTo(ref AimLookState component, in DeserializeClientData deserializeData)
			{
				component.Aim = component.Aim * 0.001f;
			}
		}

		public class SynchronizeSnapshot : ComponentSnapshotSystemBasic<AimLookState, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class Update : ComponentUpdateSystemDirect<AimLookState, Snapshot>
		{
		}
	}
}