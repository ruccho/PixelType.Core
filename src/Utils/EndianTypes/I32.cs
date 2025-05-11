using System;
using System.Runtime.CompilerServices;

namespace PixelType
{
    public struct I32
    {
        public int Internal { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(I32 self)
        {
            return Reverse(self.Internal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator I32(int self)
        {
            return new I32 { Internal = Reverse(self) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Reverse(int value)
        {
            if (BitConverter.IsLittleEndian)
            {
                var rev = ((value & 0x0000007F) << 24) |
                          ((value & 0x0000FF00) << 8) |
                          ((value & 0x00FF0000) >> 8) |
                          (((value & 0x7F000000) >> 24) +
                           (value < 0 ? 0x00000080 : 0));
                var sign = (value & 0x00000080) != 0;
                if (sign) rev = int.MinValue + rev;
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