using System.Runtime.CompilerServices;

namespace PixelType
{
    public struct I8
    {
        public sbyte Internal { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator sbyte(I8 self)
        {
            return self.Internal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator I8(sbyte self)
        {
            return new I8 { Internal = self };
        }

        public override string ToString()
        {
            return Internal.ToString();
        }
    }
}