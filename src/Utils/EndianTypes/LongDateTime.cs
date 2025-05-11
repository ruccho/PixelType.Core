using System;

namespace PixelType
{
    public struct LongDateTime
    {
        private readonly I64 value;

        private static readonly DateTime Epoch = new(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime ToDateTime()
        {
            return Epoch.Add(TimeSpan.FromSeconds(value));
        }

        public LongDateTime(DateTime dt)
        {
            value = (long)(dt - Epoch).TotalSeconds;
        }

        public override string ToString()
        {
            return ToDateTime().ToString();
        }
    }
}