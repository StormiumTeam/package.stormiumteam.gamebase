using System;

namespace GameHost.Native
{
	public unsafe struct CharBuffer256 : ICharBuffer, IEquatable<CharBuffer256>
	{
		private const int  ConstCapacity = 256;
		private fixed char buffer[ConstCapacity];

		int ICharBuffer.Capacity => ConstCapacity;
		int ICharBuffer.Length   { get; set; }

		public Span<char> Span
		{
			get
			{
				fixed (char* ptr = buffer)
					return new Span<char>(ptr, this.GetLength());
			}
		}
		
		public bool Equals(CharBuffer256 other)
		{
			return Span.SequenceEqual(other.Span);
		}

		public override bool Equals(object obj)
		{
			return obj is CharBuffer256 other && Equals(other);
		}
		
		public override int GetHashCode()
		{
			return CharBufferUtility.ComputeHashCode(this);
		}
		
		public static implicit operator CharBuffer256(string str) => CharBufferUtility.Create<CharBuffer256>(str); 
		
		public override string ToString()
		{
			return Span.ToString();
		}
	}
}