using System;

namespace GameHost.Native
{
	public unsafe struct CharBuffer128 : ICharBuffer, IEquatable<CharBuffer128>
	{
		private const int  ConstCapacity = 128;
		private fixed char buffer[ConstCapacity];

		int ICharBuffer.Capacity => ConstCapacity;
		int ICharBuffer.Length   { get; set; }

		public char* Begin
		{
			get
			{
				fixed (char* ptr = buffer)
					return ptr;
			}
		}
		
		public Span<char> Span
		{
			get
			{
				fixed (char* ptr = buffer)
					return new Span<char>(ptr, this.GetLength());
			}
		}

		public bool Equals(CharBuffer128 other)
		{
			return Span.SequenceEqual(other.Span);
		}

		public override bool Equals(object obj)
		{
			return obj is CharBuffer128 other && Equals(other);
		}
		
		public override int GetHashCode()
		{
			return CharBufferUtility.ComputeHashCode(this);
		}
		
		public static implicit operator CharBuffer128(string str) => CharBufferUtility.Create<CharBuffer128>(str); 
		
		public override string ToString()
		{
			return Span.ToString();
		}
	}
}