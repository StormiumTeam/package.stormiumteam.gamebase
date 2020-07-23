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
	}
}