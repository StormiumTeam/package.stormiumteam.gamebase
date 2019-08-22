using System;
using System.Runtime.InteropServices;
using DefaultNamespace;
using StormiumTeam.Networking.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace StormiumTeam.GameBase.Components
{
	public struct ClubSnapshotData : ISnapshotData<ClubSnapshotData>, ISnapshotFromComponent<ClubSnapshotData, ClubInformation>
	{
		[StructLayout(LayoutKind.Explicit)]
		private struct PackedColor
		{
			[FieldOffset(0)]
			public uint UInt;

			[FieldOffset(0)]
			public Color32 Color;
		}

		public uint Tick { get; set; }

		public NativeString64 Name;
		public Color32        PrimaryColor;
		public Color32        SecondaryColor;

		public void PredictDelta(uint tick, ref ClubSnapshotData baseline1, ref ClubSnapshotData baseline2)
		{
			throw new NotImplementedException();
		}

		public unsafe void Serialize(ref ClubSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			writer.WriteDelta(Name, baseline.Name, compressionModel);
			
			writer.WritePackedUIntDelta(new PackedColor {Color = PrimaryColor}.UInt, new PackedColor {Color   = baseline.PrimaryColor}.UInt, compressionModel);
			writer.WritePackedUIntDelta(new PackedColor {Color = SecondaryColor}.UInt, new PackedColor {Color = baseline.SecondaryColor}.UInt, compressionModel);
		}

		public void Deserialize(uint tick, ref ClubSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			Name = reader.ReadDelta(ref ctx, baseline.Name, compressionModel);

			var packedPrimary = reader.ReadPackedUIntDelta(ref ctx, new PackedColor {Color = baseline.PrimaryColor}.UInt, compressionModel);
			PrimaryColor = new PackedColor {UInt = packedPrimary}.Color;

			var packedSecondary = reader.ReadPackedUIntDelta(ref ctx, new PackedColor {Color = baseline.SecondaryColor}.UInt, compressionModel);
			SecondaryColor = new PackedColor {UInt = packedSecondary}.Color;
		}

		public void Interpolate(ref ClubSnapshotData target, float factor)
		{
			this = target;
		}

		public void Set(ClubInformation component)
		{
			Name           = component.Name;
			PrimaryColor   = component.PrimaryColor;
			SecondaryColor = component.SecondaryColor;
		}
	}

	public struct ClubGhostSerializer : IGhostSerializer<ClubSnapshotData>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<ClubSnapshotData>();

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			return 1;
		}

		public bool WantsPredictionDelta => false;

		public void BeginSerialize(ComponentSystemBase system)
		{
			m_ClubDescriptionType      = ComponentType.ReadWrite<ClubDescription>();
			m_ClubInformationGhostType = system.GetGhostComponentType<ClubInformation>();
		}

		private ComponentType                       m_ClubDescriptionType;
		private GhostComponentType<ClubInformation> m_ClubInformationGhostType;

		public bool CanSerialize(EntityArchetype arch)
		{
			var comps = arch.GetComponentTypes();
			var count = 0;
			for (var i = 0; i != comps.Length; i++)
			{
				if (comps[i].TypeIndex == m_ClubDescriptionType.TypeIndex
				    || comps[i] == m_ClubInformationGhostType)
					count++;
			}

			return count == 2;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref ClubSnapshotData snapshot)
		{
			var information = chunk.GetNativeArray(m_ClubInformationGhostType.Archetype)[ent];
			snapshot.Tick = tick;
			snapshot.Set(information);
		}
	}

	public class ClubGhostSpawnSystem : DefaultGhostSpawnSystem<ClubSnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(ClubSnapshotData),
				typeof(ClubDescription),
				typeof(ClubInformation),
				typeof(ReplicatedEntityComponent)
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return GetGhostArchetype();
		}
	}

	public class ClubSnapshotUpdateSystem : BaseUpdateFromSnapshotSystem<ClubSnapshotData, ClubInformation>
	{
	}
}