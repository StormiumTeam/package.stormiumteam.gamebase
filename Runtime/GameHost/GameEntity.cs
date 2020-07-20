using System;
using Unity.Entities;

namespace StormiumTeam.GameBase.GameHost.Simulation
{
	public struct GhGameEntity : IEquatable<GhGameEntity>
	{
		public uint Id;

		public bool Equals(GhGameEntity other)
		{
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is GhGameEntity other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (int) Id;
		}
	}

	public struct GhComponentType : IEquatable<GhComponentType>
	{
		public uint Id;

		public bool Equals(GhComponentType other)
		{
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is GhComponentType other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (int) Id;
		}
	}

	public readonly struct GhComponentMetadata
	{
		/// <summary>
		/// The assigned component meta
		/// </summary>
		public readonly int Assigned;

		/// <summary>
		/// Is the assignment null?
		/// </summary>
		public bool Null => Assigned == 0;
			 
		/// <summary>
		/// Is the assignment valid?
		/// </summary>
		public bool Valid => Assigned != 0;

		/// <summary>
		/// Is this a custom component?
		/// </summary>
		public bool IsShared => Assigned < 0;

		/// <summary>
		/// The reference to the non custom component
		/// </summary>
		public uint Id => IsShared ? 0 : (uint) Assigned;

		/// <summary>
		/// The reference to the entity that share the component
		/// </summary>
		public uint Entity => IsShared ? (uint) -Assigned : 0;

		public static GhComponentMetadata Reference(uint componentId) => new GhComponentMetadata((int) componentId);
		public static GhComponentMetadata Shared(uint    entity)      => new GhComponentMetadata((int) -entity);
			 
		private GhComponentMetadata(int assigned)
		{
			Assigned = assigned;
		}
	}
}