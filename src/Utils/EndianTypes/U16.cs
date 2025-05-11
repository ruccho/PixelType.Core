using System;
using System.Runtime.CompilerServices;

namespace PixelType
{
    public struct U16
    {
        public ushort Internal { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ushort(U16 self)
        {
            return Reverse(self.Internal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator U16(ushort self)
        {
            return new U16 { Internal = Reverse(self) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort Reverse(ushort value)
        {
            return BitConverter.IsLittleEndian
                ? (ushort)(((value & 0x00FF) << 8) |
                           ((value & 0xFF00) >> 8))
                : value;
        }

        public override string ToString()
        {
            return Reverse(Internal).ToString();
        }
    }
}