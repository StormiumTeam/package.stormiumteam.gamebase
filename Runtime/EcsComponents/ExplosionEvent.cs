using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;

namespace StormiumTeam.GameBase.Data
{
	public struct TargetBumpEvent : IEventData, ISerializableAsPayload
	{
		public float3 Position;
		public float3 Direction;
		public float3 Force;
		public float3 VelocityReset;

		public Entity Shooter;
		public Entity Victim;

		public void Write(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime runtime)
		{
			data.WriteValue(Position);
			data.WriteValue((half3) Direction);
			data.WriteValue((half3) Force);
			data.WriteValue((half3) VelocityReset);
			
			data.WriteDynamicIntWithMask((ulong) runtime.GetIndex(Victim), (ulong) runtime.GetIndex(Shooter));
		}

		public void Read(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
		{
			Position      = data.ReadValue<float3>();
			Direction     = data.ReadValue<half3>();
			Force         = data.ReadValue<half3>();
			VelocityReset = data.ReadValue<half3>();

			data.ReadDynIntegerFromMask(out var unsignedVictimIdx, out var unsignedShooterIdx);

			Victim  = runtime.GetWorldEntityFromGlobal((int) unsignedVictimIdx);
			Shooter = runtime.GetWorldEntityFromGlobal((int) unsignedShooterIdx);
		}

		public class Streamer : SnapshotEntityDataManualValueTypeStreamer<TargetBumpEvent>
		{
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