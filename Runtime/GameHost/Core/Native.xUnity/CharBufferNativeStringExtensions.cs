using System.Runtime.CompilerServices;
using GameHost.Native;
using Unity.Collections;

namespace GameHost.Core.Native.xUnity
{
	public static unsafe class CharBufferNativeStringExtensions
	{
		public static void CopyToNativeString<TCharBuffer>(this TCharBuffer buffer, ref FixedString32 nativeString)
			where TCharBuffer : ICharBuffer
		{
			nativeString.CopyFrom((char*) Unsafe.AsPointer(ref buffer.Span.GetPinnableReference()), (ushort) buffer.Length);
			nativeString.Length = buffer.Length;
		}

		public static void CopyToNativeString<TCharBuffer>(this TCharBuffer buffer, ref FixedString64 nativeString)
			where TCharBuffer : ICharBuffer
		{
			nativeString.CopyFrom((char*) Unsafe.AsPointer(ref buffer.Span.GetPinnableReference()), (ushort) buffer.Length);
			nativeString.Length = buffer.Length;
		}

		public static void CopyToNativeString<TCharBuffer>(this TCharBuffer buffer, ref FixedString128 nativeString)
			where TCharBuffer : ICharBuffer
		{
			nativeString.CopyFrom((char*) Unsafe.AsPointer(ref buffer.Span.GetPinnableReference()), (ushort) buffer.Length);
			nativeString.Length = buffer.Length;
		}
	}
}