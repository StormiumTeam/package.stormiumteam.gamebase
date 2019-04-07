using package.stormiumteam.networking;
using package.stormiumteam.shared;
using StormiumShared.Core.Networking;
using Unity.Entities;

namespace StormiumTeam.GameBase
{
	public static class SerializationHelper
	{
		public static bool Access<T>(ref byte mask, ref byte maskPos, SnapshotReceiver receiver, ComponentDataChangedFromEntity<T> dataFromEntity, in Entity entity, out T data)
			where T : struct, IComponentData
		{
			data = dataFromEntity[entity];
                
			if ((receiver.Flags & SnapshotFlags.FullData) != 0 || dataFromEntity.HasChange(entity))
			{
				MainBit.SetBitAt(ref mask, maskPos, 1);
				maskPos++; 
				return true;
			}

			maskPos++;
			return false;
		}
	}
}