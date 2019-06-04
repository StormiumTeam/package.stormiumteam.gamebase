using DefaultNamespace;
using package.stormiumteam.networking.runtime.lowlevel;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase.Data
{
	public struct TargetBumpEvent : IEventData, IComponentFromSnapshot<TargetBumpEvent.SnapshotData>
	{
		public struct SnapshotData : ISnapshotFromComponent<SnapshotData, TargetBumpEvent>
		{
			public uint Tick { get; private set; }

			public int3 Position;      // float * 1000
			public int3 Force;         // float * 1000 (include direction with it)
			public int3 VelocityReset; // float * 1000

			public int ShooterId;
			public int VictimId;

			public void PredictDelta(uint tick, ref SnapshotData baseline1, ref SnapshotData baseline2)
			{
			}

			public void Serialize(ref SnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedInt(Position.x, compressionModel);
				writer.WritePackedInt(Position.y, compressionModel);
				writer.WritePackedInt(Position.z, compressionModel);
				writer.WritePackedInt(Force.x, compressionModel);
				writer.WritePackedInt(Force.y, compressionModel);
				writer.WritePackedInt(Force.z, compressionModel);
				writer.WritePackedInt(VelocityReset.x, compressionModel);
				writer.WritePackedInt(VelocityReset.y, compressionModel);
				writer.WritePackedInt(VelocityReset.z, compressionModel);

				writer.WritePackedInt(ShooterId, compressionModel);
				writer.WritePackedInt(VictimId, compressionModel);
			}

			public void Deserialize(uint tick, ref SnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
			{
				Position.x      = reader.ReadPackedInt(ref ctx, compressionModel);
				Position.y      = reader.ReadPackedInt(ref ctx, compressionModel);
				Position.z      = reader.ReadPackedInt(ref ctx, compressionModel);
				Force.x         = reader.ReadPackedInt(ref ctx, compressionModel);
				Force.y         = reader.ReadPackedInt(ref ctx, compressionModel);
				Force.z         = reader.ReadPackedInt(ref ctx, compressionModel);
				VelocityReset.x = reader.ReadPackedInt(ref ctx, compressionModel);
				VelocityReset.y = reader.ReadPackedInt(ref ctx, compressionModel);
				VelocityReset.z = reader.ReadPackedInt(ref ctx, compressionModel);

				ShooterId = reader.ReadPackedInt(ref ctx, compressionModel);
				VictimId  = reader.ReadPackedInt(ref ctx, compressionModel);
			}

			public void Interpolate(ref SnapshotData target, float factor)
			{
				Position      = (int3) math.lerp(Position, target.Position, factor);
				Force         = (int3) math.lerp(Force, target.Force, factor);
				VelocityReset = (int3) math.lerp(VelocityReset, target.VelocityReset, factor);

				ShooterId = target.ShooterId;
				VictimId  = target.VictimId;
			}

			public void Set(TargetBumpEvent component)
			{
				Position      = (int3) (component.Position * 1000);
				Force         = (int3) (component.Force * 1000);
				VelocityReset = (int3) (component.VelocityReset * 1000);

				ShooterId = 0;
				VictimId  = 0;
			}
		}

		public float3 Position;
		public float3 Force;
		public float3 VelocityReset;

		public Entity Shooter;
		public Entity Victim;

		public class RegisterSerializer : AddComponentSerializer<TargetBumpEvent, SnapshotData>
		{
		}

		public class UpdateFromSnapshot : BaseUpdateFromSnapshotSystem<SnapshotData, TargetBumpEvent>
		{
		}

		public void Set(SnapshotData snapshot)
		{
			Position = (float3) snapshot.Position * 0.001f;
			Force = (float3) snapshot.Force * 0.001f;
			VelocityReset = (float3) snapshot.VelocityReset * 0.001f;
			
			Shooter = snapshot.ShooterId
		}
	}

	public struct TargetDamageEvent : IEventData, ISerializableAsPayload
	{
		public int DmgValue;

		public Entity Shooter;
		public Entity Victim;

		public void Write(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime runtime)
		{
			data.WriteDynamicIntWithMask((ulong) DmgValue, (ulong) runtime.GetIndex(Victim), (ulong) runtime.GetIndex(Shooter));
		}

		public void Read(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
		{
			data.ReadDynIntegerFromMask(out var unsignedDmgValue, out var unsignedVictimIdx, out var unsignedShooterIdx);

			DmgValue = (int) unsignedDmgValue;
			Victim   = runtime.GetWorldEntityFromGlobal((int) unsignedVictimIdx);
			Shooter  = runtime.GetWorldEntityFromGlobal((int) unsignedShooterIdx);
		}

		public class Streamer : SnapshotEntityDataManualValueTypeStreamer<TargetDamageEvent>
		{
		}
	}
}