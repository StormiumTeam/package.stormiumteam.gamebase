﻿using System;

namespace GameHost.Native
{
	public unsafe struct CharBuffer32 : ICharBuffer, IEquatable<CharBuffer32>
	{
		private const int  ConstCapacity = 32;
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

		public bool Equals(CharBuffer32 other)
		{
			return Span.SequenceEqual(other.Span);
		}

		public override bool Equals(object obj)
		{
			return obj is CharBuffer32 other && Equals(other);
		}

		public override int GetHashCode()
		{
			return CharBufferUtility.ComputeHashCode(this);
		}
		
		public static implicit operator CharBuffer32(string str) => CharBufferUtility.Create<CharBuffer32>(str); 
		
		public override string ToString()
		{
			return Span.ToString();
		}
	}
}