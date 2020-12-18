using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace Replicated.NetCode
{
	public struct SnapshotEntity : IComponentData
	{
		public uint EntityIndex;
		public uint EntityVersion;
		public int  InstigatorId;

		public short StorageEntityVersion;
		public short StorageWorldId;
		public int   StorageEntityId;

		public class Register : RegisterGameHostComponentData<SnapshotEntity>
		{
			protected override string CustomComponentPath => "GameHost.Revolution.Snapshot.Systems.Components::SnapshotEntity";
		}
	}
}