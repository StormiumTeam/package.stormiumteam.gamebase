using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace GameHost.Native
{
	public unsafe struct CharBuffer256 : ICharBuffer, IEquatable<CharBuffer256>
	{
		private const int  ConstCapacity = 256;
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
		
		public bool Equals(CharBuffer256 other)
		{
			var thisLength = this.GetLength();
			if (thisLength == 0 || thisLength != other.GetLength())
				return false;
			
			return UnsafeUtility.MemCmp(Begin, other.Begin, sizeof(char) * thisLength) == 0;
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