using Revolution;
using Revolution.NetCode;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase.Snapshots
{
	public struct ActionAmmoSnapshot : IReadWriteSnapshot<ActionAmmoSnapshot>, ISynchronizeImpl<ActionAmmo>, IPredictable<ActionAmmoSnapshot>
	{
		public struct Exclude : IComponentData
		{
		}

		public int Value, Max, Usage;

		public void WriteTo(DataStreamWriter writer, ref ActionAmmoSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			writer.WritePackedIntDelta(Value, baseline.Value, compressionModel);
			writer.WritePackedIntDelta(Max, baseline.Max, compressionModel);
			writer.WritePackedIntDelta(Usage, baseline.Usage, compressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref ActionAmmoSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			Value = reader.ReadPackedIntDelta(ref ctx, baseline.Value, compressionModel);
			Max   = reader.ReadPackedIntDelta(ref ctx, baseline.Max, compressionModel);
			Usage = reader.ReadPackedIntDelta(ref ctx, baseline.Usage, compressionModel);
		}

		public uint Tick { get; set; }

		public void SynchronizeFrom(in ActionAmmo component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
			Value = component.Value;
			Max   = component.Max;
			Usage = component.Usage;
		}

		public void SynchronizeTo(ref ActionAmmo component, in DeserializeClientData deserializeData)
		{
			component.Value = Value;
			component.Max   = Max;
			component.Usage = Usage;
		}

		public class Synchronize : ComponentSnapshotSystem_Basic_Predicted<ActionAmmo, ActionAmmoSnapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
		
		public class Update : ComponentUpdateSystemInterpolated<ActionAmmo, ActionAmmoSnapshot>
		{
			protected override bool IsPredicted => true;
		}


		public void Interpolate(ActionAmmoSnapshot target, float factor)
		{
			Value = (int) math.lerp(Value, target.Value, factor);
		}

		public void PredictDelta(uint tick, ref ActionAmmoSnapshot baseline1, ref ActionAmmoSnapshot baseline2)
		{
			var predictor = new GhostDeltaPredictor(tick, this.Tick, baseline1.Tick, baseline2.Tick);
			Value = predictor.PredictInt(Value, baseline1.Value, baseline2.Value);
		}
	}
}