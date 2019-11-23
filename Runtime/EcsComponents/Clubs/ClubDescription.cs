using System;
using System.Runtime.InteropServices;
using Revolution;
using Revolution.NetCode;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;
using Utilities;

[assembly: RegisterGenericComponentType(typeof(Relative<ClubDescription>))]

namespace StormiumTeam.GameBase.Components
{
	public struct ClubDescription : IEntityDescription
	{
	}

	public struct ClubInformation : IComponentData
	{
		public struct Exclude : IComponentData
		{
		}

		public NativeString64 Name;

		public Color PrimaryColor;
		public Color SecondaryColor;

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISynchronizeImpl<ClubInformation>, ISnapshotDelta<Snapshot>
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

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedStringDelta(Name, baseline.Name, compressionModel);

				writer.WritePackedUIntDelta(new PackedColor {Color = PrimaryColor}.UInt, new PackedColor {Color   = baseline.PrimaryColor}.UInt, compressionModel);
				writer.WritePackedUIntDelta(new PackedColor {Color = SecondaryColor}.UInt, new PackedColor {Color = baseline.SecondaryColor}.UInt, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				Name = reader.ReadPackedStringDelta(ref ctx, baseline.Name, compressionModel);

				var packedPrimary = reader.ReadPackedUIntDelta(ref ctx, new PackedColor {Color = baseline.PrimaryColor}.UInt, compressionModel);
				PrimaryColor = new PackedColor {UInt = packedPrimary}.Color;

				var packedSecondary = reader.ReadPackedUIntDelta(ref ctx, new PackedColor {Color = baseline.SecondaryColor}.UInt, compressionModel);
				SecondaryColor = new PackedColor {UInt = packedSecondary}.Color;
			}

			public void SynchronizeFrom(in ClubInformation component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				Name           = component.Name;
				PrimaryColor   = component.PrimaryColor;
				SecondaryColor = component.SecondaryColor;
			}

			public void SynchronizeTo(ref ClubInformation component, in DeserializeClientData deserializeData)
			{
				component.Name           = Name;
				component.PrimaryColor   = PrimaryColor;
				component.SecondaryColor = SecondaryColor;
			}

			public bool DidChange(Snapshot baseline)
			{
				return !(Name.Equals(baseline.Name)
				         && ColorEquals(PrimaryColor, baseline.PrimaryColor)
				         && ColorEquals(SecondaryColor, baseline.SecondaryColor));
			}

			private static bool ColorEquals(Color32 l, Color32 r)
			{
				return l.r == r.r
				       && l.g == r.g
				       && l.b == r.b
				       && l.a == r.a;
			}
		}

		public class SynchronizeSnapshot : ComponentSnapshotSystem_Delta<ClubInformation, Snapshot>
		{
			public override ComponentType   ExcludeComponent => typeof(Exclude);
			public override DeltaChangeType DeltaType        => DeltaChangeType.Both;
		}

		public class Update : ComponentUpdateSystemDirect<ClubInformation, Snapshot>
		{
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities))]
	public class ClubProvider : BaseProviderBatch<ClubProvider.Create>
	{
		[Serializable]
		public struct Create
		{
			public NativeString64 name;
			public Color          primaryColor;
			public Color          secondaryColor;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(ClubDescription),
				typeof(ClubInformation)
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, new ClubInformation
			{
				Name           = data.name,
				PrimaryColor   = data.primaryColor,
				SecondaryColor = data.secondaryColor
			});
		}
	}
}