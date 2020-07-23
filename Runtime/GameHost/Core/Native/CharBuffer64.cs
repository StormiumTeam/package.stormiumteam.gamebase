﻿using System;
using System.Runtime.CompilerServices;

namespace GameHost.Native
{
	public unsafe struct CharBuffer64 : ICharBuffer, IEquatable<CharBuffer64>
	{
		private const int  ConstCapacity = 64;
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

		public bool Equals(CharBuffer64 other)
		{
			return Span.SequenceEqual(other.Span);
		}

		public override bool Equals(object obj)
		{
			return obj is CharBuffer64 other && Equals(other);
		}

		public override int GetHashCode()
		{
			return CharBufferUtility.ComputeHashCode(this);
		}
	}
}