using System;
using System.Runtime.InteropServices;

namespace PixelType
{
    internal static class Utils
    {
        public static T Deserialize<T>(ReadOnlySpan<byte> data, int offset) where T : struct
        {
            return Deserialize<T>(data, offset, out _);
        }

        public static T Deserialize<T>(ReadOnlySpan<byte> data, int offset, out int size) where T : struct
        {
            size = Marshal.SizeOf<T>();
            var slice = data.Slice(offset, size);
            return MemoryMarshal.Read<T>(slice);
        }

        public static void Serialize<T>(T value, Span<byte> dest, int offset) where T : struct
        {
            Serialize(value, dest, offset, out _);
        }

        public static void Serialize<T>(T value, Span<byte> dest, int offset, out int size) where T : struct
        {
            size = Marshal.SizeOf<T>();
            var slice = dest.Slice(offset, size);
            MemoryMarshal.Write(slice, ref value);
        }
    }
}