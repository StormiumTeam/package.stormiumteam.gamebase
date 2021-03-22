using Unity.Profiling;

namespace StormiumTeam.GameBase.Utility.Misc.ProfilerModules
{
	public class GameHostWorldProfilerCounter
	{
		public static readonly ProfilerCategory ReceivedWorldCategory = ProfilerCategory.Scripts;

		public static readonly ProfilerCounterValue<int> ReceivedCompressedMemory = new ProfilerCounterValue<int>(ReceivedWorldCategory,
			"Received Memory (Compressed)",
			ProfilerMarkerDataUnit.Bytes);

		public static readonly ProfilerCounterValue<int> ReceivedUncompressedMemory = new ProfilerCounterValue<int>(ReceivedWorldCategory,
			"Received Memory (Uncompressed)",
			ProfilerMarkerDataUnit.Bytes);

		public static readonly ProfilerCategory NetworkCategory = ProfilerCategory.Scripts;

		public static readonly ProfilerCounterValue<int> CompressedSnapshotSize = new ProfilerCounterValue<int>(NetworkCategory,
			"Snapshot Size (Compressed)",
			ProfilerMarkerDataUnit.Bytes);

		public static readonly ProfilerCounterValue<int> UncompressedSnapshotSize = new ProfilerCounterValue<int>(NetworkCategory,
			"Snapshot Size (Uncompressed)",
			ProfilerMarkerDataUnit.Bytes);
	}
}