﻿using System;
using System.Runtime.CompilerServices;

namespace GameHost.Native.Fixed
{
	public static class FixedBufferUtility
	{
		public static int GetCapacity<TFixedBuffer>(this ref TFixedBuffer buffer)
			where TFixedBuffer : struct, IFixedBuffer
		{
			return buffer.Capacity;
		}

		public static int GetLength<TFixedBuffer>(this ref TFixedBuffer buffer)
			where TFixedBuffer : struct, IFixedBuffer
		{
			return buffer.Length;
		}

		public static void SetLength<TFixedBuffer>(this ref TFixedBuffer buffer, int length)
			where TFixedBuffer : struct, IFixedBuffer
		{
			if (length > buffer.Capacity)
				throw new IndexOutOfRangeException();
			buffer.Length = length;
		}
		
		public static int IndexOf<TFixedBuffer, TElement>(this ref TFixedBuffer buffer, TElement element)
			where TFixedBuffer : struct, IFixedBuffer<TElement>
			where TElement : IEquatable<TElement>
		{
			var span = buffer.Span;
			return span.IndexOf(element);
		}

		public static void Add<TFixedBuffer, TElement>(this ref TFixedBuffer buffer, TElement element)
			where TFixedBuffer : struct, IFixedBuffer<TElement>
			where TElement : IEquatable<TElement>
		{
			var start = buffer.Length;
			buffer.Length++;
			buffer.Span[start] = element;
		}

		public static void RemoveAt<TFixedBuffer>(this ref TFixedBuffer buffer, int index)
			where TFixedBuffer : struct, IFixedBuffer
		{
			RemoveRange(ref buffer, index, index + 1);
		}

		public static unsafe void RemoveRange<TFixedBuffer>(this ref TFixedBuffer buffer, int start, int end)
			where TFixedBuffer : struct, IFixedBuffer
		{
			var toRemove = end - start;
			if (toRemove <= 0)
				return;

			var raw      = buffer.Raw;
			var size     = buffer.ElementSize;
			var copyFrom = Math.Min(start + toRemove, buffer.Length);
			raw.Slice(copyFrom * size, (buffer.Length - copyFrom) * size).CopyTo(raw.Slice(start * size));
			buffer.Length -= toRemove;
		}

		public static TFixedBuffer Create<TFixedBuffer, T>(Span<T> span)
			where TFixedBuffer : struct, IFixedBuffer<T>
			where T : IEquatable<T>
		{
			var buffer = new TFixedBuffer();
			buffer.SetLength(span.Length);
			span.CopyTo(buffer.Span);
			return buffer;
		}

		public static int ComputeHashCode<TElement>(Span<TElement> span)
			where TElement : IEquatable<TElement>
		{
			unchecked
			{
				const int p    = 16777619;
				var       hash = (int) 2166136261;

				foreach (var tchar in span)
					hash = (hash ^ tchar.GetHashCode()) * p;

				hash += hash << 13;
				hash ^= hash >> 7;
				hash += hash << 3;
				hash ^= hash >> 17;
				hash += hash << 5;
				return hash;
			}
		}
	}
}