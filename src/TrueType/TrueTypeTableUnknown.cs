using System;

namespace PixelType.TrueType
{
    public class TrueTypeTableUnknown : TrueTypeTable
    {
        public TrueTypeTableUnknown(uint tag)
        {
            Tag = tag;
        }

        public byte[] Data { get; set; }

        public override uint Tag { get; }

        public override long GetSize()
        {
            return Data?.Length ?? 0;
        }

        public override void Serialize(Span<byte> dest)
        {
            if (GetSize() != dest.Length) throw new IndexOutOfRangeException();

            if (Data != null) Data.CopyTo(dest);
        }

        public override void Deserialize(DeserializationContext context, ref BufferReader data)
        {
            if (Data == null || Data.Length != data.Remains) Data = new byte[data.Remains];
            data.ReadBytes(Data);
        }
    }
}