﻿using System;
using System.Runtime.CompilerServices;
using GameHost.Native.Fixed;

namespace GameHost.Native
{
	public unsafe struct FixedBuffer32<T> : IFixedBuffer<T>, IEquatable<FixedBuffer32<T>>
		where T : IEquatable<T>
	{
		private const int  ConstCapacity = 32;
		private fixed byte buffer[ConstCapacity];

		int IFixedBuffer.Capacity => ConstCapacity / Unsafe.SizeOf<T>();
		int IFixedBuffer.Length   { get; set; }
		int IFixedBuffer.ElementSize => Unsafe.SizeOf<T>();
		
		Span<byte> IFixedBuffer.Raw
		{
			get
			{
				fixed (byte* ptr = buffer)
					return new Span<byte>(ptr, this.GetLength() * Unsafe.SizeOf<T>());
			}
		}

		public Span<T> Span
		{
			get
			{
				fixed (byte* ptr = buffer)
					return new Span<T>(ptr, this.GetLength());
			}
		}

		public bool Equals(FixedBuffer32<T> other)
		{
			return Span.SequenceEqual(other.Span);
		}

		public override bool Equals(object obj)
		{
			return obj is FixedBuffer32<T> other && Equals(other);
		}

		public override int GetHashCode()
		{
			return FixedBufferUtility.ComputeHashCode(Span);
		}
	}
}