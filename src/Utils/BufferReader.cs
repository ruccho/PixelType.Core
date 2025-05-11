using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PixelType
{
    public ref struct BufferReader
    {
        private readonly ReadOnlySpan<byte> buffer;
        private int cursor;

        public int Remains => buffer.Length - cursor;

        public BufferReader(ReadOnlySpan<byte> buffer)
        {
            this.buffer = buffer;
            cursor = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadUnaligned<T>(out T result) where T : unmanaged
        {
            var remains = buffer.Length - cursor;
            var size = Unsafe.SizeOf<T>();
            if (remains < size) throw new IndexOutOfRangeException();
            ref var spanRef = ref Unsafe.AsRef(buffer[cursor]);
            result = Unsafe.ReadUnaligned<T>(ref spanRef);
            cursor += size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUnaligned<T>(out T result) where T : unmanaged
        {
            var remains = buffer.Length - cursor;
            var size = Unsafe.SizeOf<T>();
            if (remains < size)
            {
                result = default;
                return false;
            }

            ref var spanRef = ref Unsafe.AsRef(buffer[cursor]);
            result = Unsafe.ReadUnaligned<T>(ref spanRef);
            cursor += size;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadBytes(Span<byte> target)
        {
            buffer.Slice(0, target.Length).CopyTo(target);
            cursor += target.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ReadBytesAsPossible()
        {
            var result = buffer.Slice(cursor);
            cursor += result.Length;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ReadBytes(int length)
        {
            var result = buffer.Slice(cursor, length);
            cursor += length;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> ReadUnaligned<T>(int numElements) where T : unmanaged
        {
            var length = numElements * Unsafe.SizeOf<T>();
            var result = MemoryMarshal.Cast<byte, T>(buffer.Slice(cursor, length));
            cursor += length;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> ReadUnalignedAsPossible<T>() where T : unmanaged
        {
            var result = MemoryMarshal.Cast<byte, T>(buffer.Slice(cursor));
            cursor += result.Length * Unsafe.SizeOf<T>();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferReader SliceFromStart(int offsetFromStart)
        {
            return new BufferReader(buffer.Slice(offsetFromStart));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferReader SliceFromStart(int offsetFromStart, int length)
        {
            return new BufferReader(buffer.Slice(offsetFromStart, length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferReader ReadBuffer(int length)
        {
            var result = new BufferReader(buffer.Slice(cursor, length));
            cursor += length;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            cursor += length;
        }
    }
}