using System;
using System.Runtime.CompilerServices;

namespace PixelType
{
    public struct I16
    {
        public short Internal { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator short(I16 self)
        {
            return Reverse(self.Internal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator I16(short self)
        {
            return new I16 { Internal = Reverse(self) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short Reverse(short value)
        {
            if (BitConverter.IsLittleEndian)
            {
                var rev = (short)(((value & 0x007F) << 8) |
                                  (((value & 0x7F00) >> 8) +
                                   (value < 0 ? 0x0080 : 0)));
                var sign = (value & 0x0080) != 0;
                if (sign) rev = (short)(short.MinValue + rev);
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