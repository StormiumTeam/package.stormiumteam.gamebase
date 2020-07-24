using System.Runtime.CompilerServices;
using GameHost.Native;
using Unity.Collections;

namespace GameHost.Core.Native.xUnity
{
	public static unsafe class CharBufferNativeStringExtensions
	{
		public static void CopyToNativeString<TCharBuffer>(this TCharBuffer buffer, FixedString32 nativeString)
			where TCharBuffer : ICharBuffer
		{
			nativeString.CopyFrom((char*) Unsafe.AsPointer(ref buffer.Span.GetPinnableReference()), (ushort) buffer.Length);
		}

		public static void CopyToNativeString<TCharBuffer>(this TCharBuffer buffer, FixedString64 nativeString)
			where TCharBuffer : ICharBuffer
		{
			nativeString.CopyFrom((char*) Unsafe.AsPointer(ref buffer.Span.GetPinnableReference()), (ushort) buffer.Length);
		}

		public static void CopyToNativeString<TCharBuffer>(this TCharBuffer buffer, FixedString128 nativeString)
			where TCharBuffer : ICharBuffer
		{
			nativeString.CopyFrom((char*) Unsafe.AsPointer(ref buffer.Span.GetPinnableReference()), (ushort) buffer.Length);
		}
	}
}