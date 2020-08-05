using System;

namespace GameHost.Native
{
	public static class CharBufferUtility
	{
		public static int GetLength<TCharBuffer>(this TCharBuffer buffer)
			where TCharBuffer : unmanaged, ICharBuffer
		{
			return buffer.Length;
		}

		public static void SetLength<TCharBuffer>(this ref TCharBuffer buffer, int length)
			where TCharBuffer : unmanaged, ICharBuffer
		{
			if (length > buffer.Capacity)
				throw new IndexOutOfRangeException($"{length} > {buffer.Capacity}");
			buffer.Length = length;
		}

		public static TCharBuffer Create<TCharBuffer>(string content)
			where TCharBuffer : unmanaged, ICharBuffer
		{
			var buffer = default(TCharBuffer);
			if (string.IsNullOrEmpty(content))
				return buffer;
			
			var span   = content.AsSpan();
			buffer.SetLength(span.Length);
			span.CopyTo(buffer.Span);
			return buffer;
		}
		
		public static TCharBuffer Create<TCharBuffer>(Span<char> content)
			where TCharBuffer : unmanaged, ICharBuffer
		{
			var buffer = default(TCharBuffer);
			if (content.Length == 0)
				return buffer;
			
			buffer.SetLength(content.Length);
			content.CopyTo(buffer.Span);
			return buffer;
		}

		public static int ComputeHashCode<TCharBuffer>(TCharBuffer buffer)
			where TCharBuffer : unmanaged, ICharBuffer
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