using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;

namespace StormiumTeam.GameBase.Data
{
	public struct TargetExplosionEvent : IEventData, ISerializableAsPayload
	{
		public float3 Position;
		public float3 Direction;
		public float3 Force;

		public Entity Shooter;
		public Entity Victim;

		public void Write(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime runtime)
		{
			data.WriteUnmanaged(this);
		}

		public void Read(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
		{
			this = data.ReadValue<TargetExplosionEvent>();

			Shooter = runtime.EntityToWorld(Shooter);
			Victim  = runtime.EntityToWorld(Victim);
		}

		public class Streamer : SnapshotEntityDataManualValueTypeStreamer<TargetExplosionEvent>
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
			data.WriteUnmanaged(this);
		}

		public void Read(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
		{
			this = data.ReadValue<TargetDamageEvent>();

			Shooter = runtime.EntityToWorld(Shooter);
			Victim = runtime.EntityToWorld(Victim);
		}
		
		public class Streamer : SnapshotEntityDataManualValueTypeStreamer<TargetDamageEvent>
		{
		}
	}
}