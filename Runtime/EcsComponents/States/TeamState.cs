using StormiumTeam.GameBase;
using StormiumTeam.Networking.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(Relative<TeamDescription>))]

namespace StormiumTeam.GameBase
{
	public struct TeamDescription : IEntityDescription
	{
	}

	public struct TeamEmptySnapshotData : ISnapshotData<TeamEmptySnapshotData>
	{
		public uint Tick { get; set; }

		public void PredictDelta(uint tick, ref TeamEmptySnapshotData baseline1, ref TeamEmptySnapshotData baseline2)
		{
		}

		public void Serialize(ref TeamEmptySnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
		}

		public void Deserialize(uint tick, ref TeamEmptySnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;
		}

		public void Interpolate(ref TeamEmptySnapshotData target, float factor)
		{
		}
	}

	public struct TeamEmptyGhostSerializer : IGhostSerializer<TeamEmptySnapshotData>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<TeamEmptySnapshotData>();

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			return 1;
		}

		public bool WantsPredictionDelta => false;

		public GhostComponentType<TeamDescription> TeamDescriptionGhostType;

		public void BeginSerialize(ComponentSystemBase system)
		{
			system.GetGhostComponentType(out TeamDescriptionGhostType);
		}

		public bool CanSerialize(EntityArchetype arch)
		{
			var matches = 0;
			var comps   = arch.GetComponentTypes();
			for (var i = 0; i != comps.Length; i++)
			{
				if (comps[i] == TeamDescriptionGhostType) matches++;
			}

			return matches == 1;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref TeamEmptySnapshotData snapshot)
		{
			snapshot.Tick = tick;
		}
	}

	public class TeamEmptyGhostSpawnSystem : DefaultGhostSpawnSystem<TeamEmptySnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(TeamDescription),
				typeof(TeamEmptySnapshotData),
				typeof(ReplicatedEntityComponent)
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(TeamDescription),
				typeof(TeamEmptySnapshotData),
				typeof(ReplicatedEntityComponent),
				typeof(PredictedEntityComponent)
			);
		}
	}
}