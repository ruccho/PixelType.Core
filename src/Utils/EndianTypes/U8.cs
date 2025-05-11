using System.Runtime.CompilerServices;

namespace PixelType
{
    public struct U8
    {
        public byte Internal { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte(U8 self)
        {
            return self.Internal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator U8(byte self)
        {
            return new U8 { Internal = self };
        }

        public override string ToString()
        {
            return Internal.ToString();
        }
    }
}