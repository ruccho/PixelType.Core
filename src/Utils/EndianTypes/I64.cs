using System;
using System.Runtime.CompilerServices;

namespace PixelType
{
    public struct I64
    {
        public long Internal { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(I64 self)
        {
            return Reverse(self.Internal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator I64(long self)
        {
            return new I64 { Internal = Reverse(self) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long Reverse(long value)
        {
            if (BitConverter.IsLittleEndian)
            {
                var rev = ((value & 0x000000000000007F) << 56) |
                          ((value & 0x000000000000FF00) << 40) |
                          ((value & 0x0000000000FF0000) << 24) |
                          ((value & 0x00000000FF000000) << 8) |
                          ((value & 0x000000FF00000000) >> 8) |
                          ((value & 0x0000FF0000000000) >> 24) |
                          ((value & 0x00FF000000000000) >> 40) |
                          (((value & 0x7F00000000000000) >> 56) +
                           (value < 0 ? 0x0000000000000080 : 0));
                var sign = (value & 0x0000000000000080) != 0;
                if (sign) rev = long.MinValue + rev;
                return rev;
            }

            return value;
        }

        public override string ToString()
        {
            return Reverse(Internal).ToString();
        }
    }
}