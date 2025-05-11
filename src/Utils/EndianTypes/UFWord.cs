using System.Runtime.CompilerServices;

namespace PixelType
{
    public struct UFWord
    {
        private U16 value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ushort(UFWord self)
        {
            return self.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UFWord(ushort self)
        {
            return new UFWord { value = self };
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}