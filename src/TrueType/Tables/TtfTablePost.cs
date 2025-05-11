using System;
using System.Runtime.InteropServices;

namespace PixelType.TrueType
{
    public class TtfTablePost : TrueTypeTable
    {
        public override uint Tag => TrueTypeFont.ToTableTag("post");

        public TtfTablePostHeaderData Header { get; set; }
        public byte[] Body { get; set; } = Array.Empty<byte>();

        public override long GetSize()
        {
            return Marshal.SizeOf<TtfTablePostHeaderData>() + Body.Length;
        }

        public override void Serialize(Span<byte> dest)
        {
            if (dest.Length != GetSize()) throw new ArgumentException();

            Utils.Serialize(Header, dest, 0, out var headerSize);
            Body.CopyTo(dest.Slice(headerSize));
        }

        public override void Deserialize(DeserializationContext context, ref BufferReader data)
        {
            data.ReadUnaligned(out TtfTablePostHeaderData header);
            Header = header;
            var format = Header.format.ToInt32();

            if (format != 0x00010000 &&
                format != 0x00020000 &&
                format != 0x00025000 &&
                format != 0x00030000 &&
                format != 0x00040000) throw new NotSupportedException();

            var hasAdditionalData = data.Remains > 0;
            if (hasAdditionalData)
            {
                if (format != 0x00020000 && format != 0x00025000) throw new ArgumentException();

                Body = data.ReadBytesAsPossible().ToArray();
            }
            else
            {
                if (format == 0x00020000 || format == 0x00025000) throw new ArgumentException();
                Body = Array.Empty<byte>();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTablePostHeaderData
    {
        public Fixed format;
        public Fixed italicAngle;
        public FWord underlinePosition;
        public FWord underlineThickness;
        public U32 isFixedPitch;
        public U32 minMemType42;
        public U32 maxMemType42;
        public U32 minMemType1;
        public U32 maxMemType1;
    }
}