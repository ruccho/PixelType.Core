using System.Runtime.CompilerServices;

namespace PixelType
{
    public struct FWord
    {
        private I16 value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator short(FWord self)
        {
            return self.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FWord(short self)
        {
            return new FWord { value = self };
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}