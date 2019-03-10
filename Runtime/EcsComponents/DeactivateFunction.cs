using StormiumShared.Core.Networking;
using Unity.Entities;

namespace StormiumTeam.GameBase.Data
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