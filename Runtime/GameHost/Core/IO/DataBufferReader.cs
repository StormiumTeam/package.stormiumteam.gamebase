using System;
using System.Runtime.CompilerServices;
using GameHost.Native;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace RevolutionSnapshot.Core.Buffers
{
	public unsafe struct DataBufferReader
	{
		[NativeDisableUnsafePtrRestriction]
		public byte* DataPtr;

		public int CurrReadIndex;
		public int Length;

		public DataBufferReader(IntPtr dataPtr, int length) : this((byte*) dataPtr, length)
		{
		}

		public DataBufferReader(byte* dataPtr, int length)
		{
			if (dataPtr == null)
				throw new InvalidOperationException("dataPtr is null");

			DataPtr       = dataPtr;
			CurrReadIndex = 0;
			Length        = length;
		}

		public DataBufferReader(DataBufferReader reader, int start, int end)
		{
			DataPtr       = (byte*) ((IntPtr) reader.DataPtr + start);
			CurrReadIndex = 0;
			Length        = end - start;
		}

		public int GetReadIndex(DataBufferMarker marker)
		{
			var readIndex = !marker.Valid ? CurrReadIndex : marker.Index;
			if (readIndex >= Length) throw new IndexOutOfRangeException("p1");

			return readIndex;
		}

		public DataBufferReader(Span<byte> data)
		{
			DataPtr       = (byte*) Unsafe.AsPointer(ref data.GetPinnableReference());
			CurrReadIndex = 0;
			Length        = data.Length;
		}

		public int GetReadIndexAndSetNew(DataBufferMarker marker, int size)
		{
			var readIndex = !marker.Valid ? CurrReadIndex : marker.Index;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			if (readIndex >= Length)
				throw new IndexOutOfRangeException($"p1");
#endif

			CurrReadIndex = readIndex + size;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			if (CurrReadIndex > Length)
				throw new IndexOutOfRangeException($"p2");
#endif

			return readIndex;
		}

		public void ReadUnsafe(byte* data, int index, int size)
		{
			UnsafeUtility.MemCpy(data, (void*) IntPtr.Add((IntPtr) DataPtr, index), (uint) size);
		}

		public void ReadDataSafe(byte* data, int size, DataBufferMarker marker = default)
		{
			var readIndex = GetReadIndexAndSetNew(marker, size);
			// Set it for later usage
			CurrReadIndex = readIndex + size;
			// Read the value
			ReadUnsafe(data, readIndex, size);
		}

		public void ReadDataSafe<T>(NativeArray<T> array, DataBufferMarker marker = default)
			where T : struct
		{
			ReadDataSafe((byte*) array.GetUnsafePtr(), array.Length * UnsafeUtility.SizeOf<T>(), marker);
		}
		
		public void ReadDataSafe<T>(Span<T> span, DataBufferMarker marker = default)
			where T : struct
		{
			ReadDataSafe((byte*) Unsafe.AsPointer(ref span.GetPinnableReference()), span.Length * Unsafe.SizeOf<T>(), marker);
		}

		public T ReadValue<T>(DataBufferMarker marker = default)
			where T : struct
		{
			var val       = default(T);
			var size      = UnsafeUtility.SizeOf<T>();
			var readIndex = GetReadIndexAndSetNew(marker, size);

			// Set it for later usage
			CurrReadIndex = readIndex + size;
			// Read the value
			ReadUnsafe((byte*) UnsafeUtility.AddressOf(ref val), readIndex, size);

			return val;
		}

		public DataBufferMarker CreateMarker(int index)
		{
			return new DataBufferMarker(index);
		}

		public ulong ReadDynInteger(DataBufferMarker marker = default)
		{
			var byteCount = ReadValue<byte>();

			if (byteCount == 0) return 0;
			if (byteCount == sizeof(byte)) return ReadValue<byte>();
			if (byteCount == sizeof(ushort)) return ReadValue<ushort>();
			if (byteCount == sizeof(uint)) return ReadValue<uint>();
			if (byteCount == sizeof(ulong)) return ReadValue<ulong>();

			throw new InvalidOperationException($"Expected byte count range: [{sizeof(byte)}..{sizeof(ulong)}], received: {byteCount}");
		}

		public void ReadDynIntegerFromMask(out ulong r1, out ulong r2)
		{
			void getval(ref DataBufferReader data, int mr, ref ulong i)
			{
				if (mr == 0) i = data.ReadValue<byte>();
				if (mr == 1) i = data.ReadValue<ushort>();
				if (mr == 2) i = data.ReadValue<uint>();
				if (mr == 3) i = data.ReadValue<ulong>();
			}

			var mask = ReadValue<byte>();
			var val1 = mask & 3;
			var val2 = (mask & 12) >> 2;

			r1 = default;
			r2 = default;

			getval(ref this, val1, ref r1);
			getval(ref this, val2, ref r2);
		}

		public void ReadDynIntegerFromMask(out ulong r1, out ulong r2, out ulong r3)
		{
			void getval(ref DataBufferReader data, int mr, ref ulong i)
			{
				if (mr == 0) i = data.ReadValue<byte>();
				if (mr == 1) i = data.ReadValue<ushort>();
				if (mr == 2) i = data.ReadValue<uint>();
				if (mr == 3) i = data.ReadValue<ulong>();
			}

			var mask = ReadValue<byte>();
			var val1 = mask & 3;
			var val2 = (mask & 12) >> 2;
			var val3 = (mask & 48) >> 4;

			r1 = default;
			r2 = default;
			r3 = default;

			getval(ref this, val1, ref r1);
			getval(ref this, val2, ref r2);
			getval(ref this, val3, ref r3);
		}

		public void ReadDynIntegerFromMask(out ulong r1, out ulong r2, out ulong r3, out ulong r4)
		{
			void getval(ref DataBufferReader data, int mr, ref ulong i)
			{
				if (mr == 0) i = data.ReadValue<byte>();
				if (mr == 1) i = data.ReadValue<ushort>();
				if (mr == 2) i = data.ReadValue<uint>();
				if (mr == 3) i = data.ReadValue<ulong>();
			}

			var mask = ReadValue<byte>();
			var val1 = mask & 3;
			var val2 = (mask & 12) >> 2;
			var val3 = (mask & 48) >> 4;
			var val4 = (mask & 192) >> 6;

			r1 = default;
			r2 = default;
			r3 = default;
			r4 = default;

			getval(ref this, val1, ref r1);
			getval(ref this, val2, ref r2);
			getval(ref this, val3, ref r3);
			getval(ref this, val3, ref r4);
		}

		public string ReadString(DataBufferMarker marker = default(DataBufferMarker))
		{
			var length = ReadValue<int>();
			if (length < 1024)
			{
				Span<char> span = stackalloc char[length];
				ReadDataSafe(new Span<char>(Unsafe.AsPointer(ref span.GetPinnableReference()), span.Length), marker);
				return new string((char*) Unsafe.AsPointer(ref span.GetPinnableReference()), 0, length);
			}

			var ptr = UnsafeUtility.Malloc(length * sizeof(char), UnsafeUtility.AlignOf<char>(), Allocator.Temp);
			ReadDataSafe((byte*) ptr, length * sizeof(char), marker);
			UnsafeUtility.Free(ptr, Allocator.Temp);
			return new string((char*) ptr, 0, length);
		}

		public TCharBuffer ReadBuffer<TCharBuffer>(DataBufferMarker marker = default(DataBufferMarker))
			where TCharBuffer : unmanaged, ICharBuffer
		{
			var length = ReadValue<int>();
			using var array  = new NativeArray<char>(length, Allocator.Temp);
			ReadDataSafe(array, marker);
			return CharBufferUtility.Create<TCharBuffer>(array);
		}
	}
}