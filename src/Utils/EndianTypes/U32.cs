using System;
using System.Runtime.CompilerServices;

namespace PixelType
{
    public struct U32
    {
        public uint Internal { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator uint(U32 self)
        {
            return Reverse(self.Internal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator U32(uint self)
        {
            return new U32 { Internal = Reverse(self) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Reverse(uint value)
        {
            return BitConverter.IsLittleEndian
                ? ((value & 0x000000FF) << 24) |
                  ((value & 0x0000FF00) << 8) |
                  ((value & 0x00FF0000) >> 8) |
                  ((value & 0xFF000000) >> 24)
                : value;
        }

        public override string ToString()
        {
            return Reverse(Internal).ToString();
        }
    }
}