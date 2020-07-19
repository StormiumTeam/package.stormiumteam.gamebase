using Revolution;
using Revolution.Utils;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;

[assembly: RegisterGenericComponentType(typeof(Predicted<SVelocity.SnapshotData>))]

namespace StormiumTeam.GameBase
{
	public struct SVelocity : IComponentData
	{
		public struct Exclude : IComponentData
		{
		}

		public struct SnapshotData : IReadWriteSnapshot<SnapshotData>, ISynchronizeImpl<SVelocity>, IPredictable<SnapshotData>
		{
			public const int   Quantization   = 100;
			public const float DeQuantization = 1 / 100f;

			public uint Tick { get; set; }

			public QuantizedFloat3 Velocity; // float * 1000

			public void WriteTo(DataStreamWriter writer, ref SnapshotData baseline, NetworkCompressionModel compressionModel)
			{
				for (var i = 0; i != 3; i++)
					writer.WritePackedIntDelta(Velocity[i], baseline.Velocity[i], compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref SnapshotData baseline, NetworkCompressionModel compressionModel)
			{
				for (var i = 0; i != 3; i++)
					Velocity[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Velocity[i], compressionModel);
			}

			public void SynchronizeFrom(in SVelocity component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				Velocity.Set(Quantization, component.Value);
			}

			public void SynchronizeTo(ref SVelocity component, in DeserializeClientData deserializeData)
			{
				component.Value = Velocity.Get(DeQuantization);
			}

			public void Interpolate(SnapshotData target, float factor)
			{
				Velocity.Result = (int3) math.lerp(Velocity.Result, target.Velocity.Result, factor);
			}

			public void PredictDelta(uint tick, ref SnapshotData baseline1, ref SnapshotData baseline2)
			{
				var predictor                                   = new GhostDeltaPredictor(tick, Tick, baseline1.Tick, baseline2.Tick);
				for (var i = 0; i != 3; i++) Velocity.Result[i] = predictor.PredictInt(Velocity.Result[i], baseline1.Velocity.Result[i], baseline2.Velocity.Result[i]);
			}
		}

		public float3 Value;

		public float3 normalized => math.normalizesafe(Value);
		public float  speed      => math.length(Value);
		public float  speedSqr   => math.lengthsq(Value);

		public float3 xfz => new float3(Value.x, 0, Value.z);

		public SVelocity(float3 value)
		{
			Value = value;
		}

		public class System : ComponentSnapshotSystemBasic<SVelocity, SnapshotData>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class Synchronize : ComponentUpdateSystemInterpolated<SVelocity, SnapshotData>
		{
			protected override bool IsPredicted => false;
		}
	}
}