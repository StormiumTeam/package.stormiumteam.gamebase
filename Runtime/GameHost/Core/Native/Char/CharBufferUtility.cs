using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace GameHost.Native
{
	public static class CharBufferUtility
	{
		public static int GetLength<TCharBuffer>(this TCharBuffer buffer)
			where TCharBuffer : unmanaged, ICharBuffer
		{
			return buffer.Length;
		}

		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		private static void TryCapacityOrThrow(int length, int capacity)
		{
			if (length > capacity)
				throw new IndexOutOfRangeException($"{length} > {capacity}");
		}

		public static void SetLength<TCharBuffer>(this ref TCharBuffer buffer, int length)
			where TCharBuffer : unmanaged, ICharBuffer
		{
			TryCapacityOrThrow(length, buffer.Capacity);
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

		public static unsafe TCharBuffer Create<TCharBuffer>(NativeArray<char> content)
			where TCharBuffer : unmanaged, ICharBuffer
		{
			var buffer = default(TCharBuffer);
			if (content.Length == 0)
				return buffer;

			buffer.SetLength(content.Length);
			UnsafeUtility.MemCpy(buffer.Begin, content.GetUnsafeReadOnlyPtr(), sizeof(char) * content.Length);
			return buffer;
		}

		public static unsafe int ComputeHashCode<TCharBuffer>(TCharBuffer buffer)
			where TCharBuffer : unmanaged, ICharBuffer
		{
			unchecked
			{
				const int p    = 16777619;
				var       hash = (int) 2166136261;

				var tchar = buffer.Begin;
				for (var i = 0; i < buffer.Length; i++)
					hash = (hash ^ tchar[i]) * p;

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