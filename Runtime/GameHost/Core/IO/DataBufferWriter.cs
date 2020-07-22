using System;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace RevolutionSnapshot.Core.Buffers
{
    public struct DataBufferMarker
    {
        public bool Valid;
        public int  Index;

        public DataBufferMarker(int index)
        {
            Index = index;
            Valid = true;
        }

        public DataBufferMarker GetOffset(int offset)
        {
            return new DataBufferMarker(Index + offset);
        }
    }

    public unsafe partial struct DataBufferWriter : IDisposable
    {
        internal struct DataBuffer
        {
            public byte* buffer;
            public int   length;
            public int   capacity;
        }

        private Allocator m_Allocator;
        
        [NativeDisableUnsafePtrRestriction]
        private DataBuffer* m_Data;

        public int Length
        {
            get => m_Data->length;
            set => m_Data->length = value;
        }

        public int Capacity
        {
            get => m_Data->capacity;
            set
            {
                var dataCapacity = m_Data->capacity;
                if (dataCapacity == value)
                    return;

                if (dataCapacity > value)
                    throw new InvalidOperationException("New capacity is shorter than current one");

                var newBuffer = (byte*) UnsafeUtility.Malloc(value, UnsafeUtility.AlignOf<byte>(), m_Allocator);

                UnsafeUtility.MemCpy(newBuffer, m_Data->buffer, m_Data->length);
                UnsafeUtility.Free(m_Data->buffer, m_Allocator);

                m_Data->buffer   = newBuffer;
                m_Data->capacity = value;
            }
        }

        public IntPtr GetSafePtr() => (IntPtr) m_Data->buffer;


        public DataBufferWriter(int capacity, Allocator allocator)
        {
            m_Allocator = allocator;

            m_Data           = (DataBuffer*) UnsafeUtility.Malloc(sizeof(DataBuffer), UnsafeUtility.AlignOf<DataBuffer>(), allocator);
            m_Data->buffer   = (byte*) UnsafeUtility.Malloc(capacity, UnsafeUtility.AlignOf<byte>(), allocator);
            m_Data->length   = 0;
            m_Data->capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetWriteInfo(int size, DataBufferMarker marker)
        {
            var writeIndex = marker.Valid ? marker.Index : m_Data->length;

            TryResize(writeIndex + size);

            return writeIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryResize(int newCapacity)
        {
            if (m_Data->capacity >= newCapacity) return;

            Capacity = newCapacity * 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteData(byte* data, int index, int length)
        {
            UnsafeUtility.MemCpy(m_Data->buffer + index, data, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteDataSafe(byte* data, int writeSize, DataBufferMarker marker)
        {
            int dataLength = m_Data->length,
                writeIndex = marker.Valid ? marker.Index : dataLength;

            // Copy from GetWriteInfo()

            var predictedLength = writeIndex + writeSize;

            // Copy from TryResize()
            if (m_Data->capacity <= predictedLength)
            {
                Capacity = predictedLength * 2;
            }

            // Copy from WriteData()
            UnsafeUtility.MemCpy(m_Data->buffer + writeIndex, data, writeSize);

            m_Data->length = Math.Max(predictedLength, dataLength);

            var rm = default(DataBufferMarker);
            rm.Valid = true;
            rm.Index = writeIndex;

            return rm;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteArray<T>(NativeArray<T> span, DataBufferMarker marker = default) 
            where T : struct
        {
            return WriteDataSafe((byte*) span.GetUnsafePtr(), UnsafeUtility.SizeOf<T>() * span.Length, marker);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteSpan<T>(Span<T> span, DataBufferMarker marker = default)
        {
            return WriteDataSafe((byte*) Unsafe.AsPointer(ref span.GetPinnableReference()), Unsafe.SizeOf<T>() * span.Length, marker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteRef<T>(ref T val, DataBufferMarker marker = default(DataBufferMarker))
            where T : struct
        {
            return WriteDataSafe((byte*) UnsafeUtility.AddressOf(ref val), UnsafeUtility.SizeOf<T>(), marker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteUnmanaged<T>(T val, DataBufferMarker marker = default(DataBufferMarker))
            where T : unmanaged
        {
            return WriteDataSafe((byte*) &val, sizeof(T), marker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteValue<T>(T val, DataBufferMarker marker = default(DataBufferMarker))
            where T : struct
        {
            return WriteDataSafe((byte*) UnsafeUtility.AddressOf(ref val), UnsafeUtility.SizeOf<T>(), marker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker CreateMarker(int index)
        {
            DataBufferMarker marker = default;
            marker.Valid = true;
            marker.Index = index;
            return marker;
        }

        public void Dispose()
        {
            UnsafeUtility.Free(m_Data->buffer, m_Allocator);
            UnsafeUtility.Free(m_Data, m_Allocator);

            m_Data = null;
        }
    }

    public unsafe partial struct DataBufferWriter
    {
        public DataBufferMarker WriteByte(byte val, DataBufferMarker marker = default(DataBufferMarker))
        {
            return WriteDataSafe((byte*) &val, sizeof(byte), marker);
        }

        public DataBufferMarker WriteShort(short val, DataBufferMarker marker = default(DataBufferMarker))
        {
            return WriteDataSafe((byte*) &val, sizeof(short), marker);
        }

        public DataBufferMarker WriteInt(int val, DataBufferMarker marker = default(DataBufferMarker))
        {
            return WriteDataSafe((byte*) &val, sizeof(int), marker);
        }

        public DataBufferMarker WriteLong(long val, DataBufferMarker marker = default(DataBufferMarker))
        {
            return WriteDataSafe((byte*) &val, sizeof(long), marker);
        }

        public DataBufferMarker WriteString(string val, Encoding encoding = null, DataBufferMarker marker = default(DataBufferMarker))
        {
            fixed (char* strPtr = val)
            {
                return WriteString(strPtr, val.Length, encoding, marker);
            }
        }

        public DataBufferMarker WriteString(char* val, int strLength, Encoding encoding = null, DataBufferMarker marker = default(DataBufferMarker))
        {
            throw new NotImplementedException("crash");
        }

        public void WriteDynamicInt(ulong integer)
        {
            if (integer == 0)
            {
                WriteUnmanaged<byte>((byte) 0);
            }
            else if (integer <= byte.MaxValue)
            {
                WriteByte((byte) sizeof(byte));
                WriteUnmanaged<byte>((byte) integer);
            }
            else if (integer <= ushort.MaxValue)
            {
                WriteByte((byte) sizeof(ushort));
                WriteUnmanaged((ushort) integer);
            }
            else if (integer <= uint.MaxValue)
            {
                WriteByte((byte) sizeof(uint));
                WriteUnmanaged((uint) integer);
            }
            else
            {
                WriteByte((byte) sizeof(ulong));
                WriteUnmanaged(integer);
            }
        }

        public void WriteDynamicIntWithMask(in ulong r1, in ulong r2)
        {
            byte setval(ref DataBufferWriter data, in ulong i)
            {
                if (i <= byte.MaxValue)
                {
                    data.WriteUnmanaged((byte) i);
                    return 0;
                }

                if (i <= ushort.MaxValue)
                {
                    data.WriteUnmanaged((ushort) i);
                    return 1;
                }

                if (i <= uint.MaxValue)
                {
                    data.WriteUnmanaged((uint) i);
                    return 2;
                }

                data.WriteUnmanaged(i);
                return 3;
            }

            var maskMarker = WriteByte(0);
            var m1         = setval(ref this, r1);
            var m2         = setval(ref this, r2);

            WriteByte((byte) (m1 | (m2 << 2)), maskMarker);
        }

        public void WriteDynamicIntWithMask(in ulong r1, in ulong r2, in ulong r3)
        {
            byte setval(ref DataBufferWriter data, in ulong i)
            {
                if (i <= byte.MaxValue)
                {
                    data.WriteUnmanaged((byte) i);
                    return 0;
                }

                if (i <= ushort.MaxValue)
                {
                    data.WriteUnmanaged((ushort) i);
                    return 1;
                }

                if (i <= uint.MaxValue)
                {
                    data.WriteUnmanaged((uint) i);
                    return 2;
                }

                data.WriteUnmanaged(i);
                return 3;
            }

            var maskMarker = WriteByte(0);
            var m1         = setval(ref this, r1);
            var m2         = setval(ref this, r2);
            var m3         = setval(ref this, r3);

            WriteByte((byte) (m1 | (m2 << 2) | (m3 << 4)), maskMarker);
        }

        public void WriteDynamicIntWithMask(in ulong r1, in ulong r2, in ulong r3, in ulong r4)
        {
            byte setval(ref DataBufferWriter data, in ulong i)
            {
                if (i <= byte.MaxValue)
                {
                    data.WriteUnmanaged((byte) i);
                    return 0;
                }

                if (i <= ushort.MaxValue)
                {
                    data.WriteUnmanaged((ushort) i);
                    return 1;
                }

                if (i <= uint.MaxValue)
                {
                    data.WriteUnmanaged((uint) i);
                    return 2;
                }

                data.WriteUnmanaged(i);
                return 3;
            }

            var maskMarker = WriteByte(0);
            var m1         = setval(ref this, r1);
            var m2         = setval(ref this, r2);
            var m3         = setval(ref this, r3);
            var m4         = setval(ref this, r4);

            WriteByte((byte) (m1 | (m2 << 2) | (m3 << 4) | (m4 << 6)), maskMarker);
        }

        public void WriteBuffer(DataBufferWriter dataBuffer)
        {
            WriteDataSafe((byte*) dataBuffer.GetSafePtr(), dataBuffer.Length, default(DataBufferMarker));
        }

        public void WriteStaticString(string val, Encoding encoding = null)
        {
            fixed (char* strPtr = val)
            {
                WriteStaticString(strPtr, val.Length, encoding);
            }
        }

        public void WriteStaticString(char* val, int strLength, Encoding encoding = null)
        {
            // If we have a null encoding, let's get the most used one (UTF8)
            encoding = encoding ?? Encoding.UTF8;

            void* tempCpyPtr = null;

            try
            {
                // Get the length of a 'UTF8 char' * 'string length';
                var cpyLength = encoding.GetMaxByteCount(strLength);
                // Allocate a temp memory region, and then...
                tempCpyPtr = UnsafeUtility.Malloc(cpyLength, 4, Allocator.Temp);
                // ... Get the bytes from the char array
                encoding.GetBytes(val, strLength, (byte*) tempCpyPtr, cpyLength);

                // Write the length of the string to the current index of the buffer
                WriteInt(cpyLength);
                var endMarker = WriteInt(0);
                // Write the string buffer data
                WriteInt(strLength); // In future, we should get a better way to define that
                WriteDataSafe((byte*) tempCpyPtr, cpyLength, default);
                // Re-write the end integer from end marker
                var l = Length;
                WriteInt(Length, endMarker);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                // If we had no problem with our temporary allocation, free it.
                if (tempCpyPtr != null)
                    UnsafeUtility.Free(tempCpyPtr, Allocator.Temp);
            }
        }
    }
}