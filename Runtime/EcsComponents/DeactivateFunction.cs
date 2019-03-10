using StormiumShared.Core.Networking;
using Unity.Entities;

namespace Runtime.Data
{
	public struct DeactivateMovement : IComponentData
	{
		public class Streamer : SnapshotEntityComponentStatusStreamer<DeactivateMovement>
		{
		}
	}

	public struct DeactivateInput : IComponentData
	{
		public class Streamer : SnapshotEntityComponentStatusStreamer<DeactivateMovement>
		{
		}
	}
}