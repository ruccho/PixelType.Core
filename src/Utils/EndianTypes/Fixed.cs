namespace PixelType
{
    public struct Fixed
    {
        private I32 value;

        public int ToInt32()
        {
            return value;
        }

        public static Fixed FromInt32(int value)
        {
            return new Fixed { value = value };
        }

        public override string ToString()
        {
            return ((int)value).ToString("X8");
        }
    }
}