﻿using System;

namespace GameHost.Native
{
	public static class CharBufferUtility
	{
		public static int GetLength<TCharBuffer>(this TCharBuffer buffer)
			where TCharBuffer : struct, ICharBuffer
		{
			return buffer.Length;
		}

		public static void SetLength<TCharBuffer>(this TCharBuffer buffer, int length)
			where TCharBuffer : struct, ICharBuffer
		{
			if (length > buffer.Capacity)
				throw new IndexOutOfRangeException();
			buffer.Length = length;
		}

		public static TCharBuffer Create<TCharBuffer>(string content)
			where TCharBuffer : struct, ICharBuffer
		{
			var buffer = new TCharBuffer();
			var span   = content.AsSpan();
			buffer.SetLength(span.Length);
			span.CopyTo(buffer.Span);
			return buffer;
		}

		public static int ComputeHashCode<TCharBuffer>(TCharBuffer buffer)
			where TCharBuffer : struct, ICharBuffer
		{
			unchecked
			{
				const int p    = 16777619;
				var       hash = (int) 2166136261;

				var span = buffer.Span;
				foreach (var tchar in span)
					hash = (hash ^ tchar) * p;

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