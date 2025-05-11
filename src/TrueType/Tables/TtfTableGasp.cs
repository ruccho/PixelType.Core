using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PixelType.TrueType
{
    public class TtfTableGasp : TrueTypeTable
    {
        public override uint Tag { get; } = TrueTypeFont.ToTableTag("gasp");

        public TtfTableGaspHeaderData Header { get; set; }
        public List<TtfTableGaspRangeData> Ranges { get; } = new();

        public override long GetSize()
        {
            return Unsafe.SizeOf<TtfTableGaspHeaderData>() + Unsafe.SizeOf<TtfTableGaspRangeData>() * Ranges.Count;
        }

        public override void Serialize(Span<byte> dest)
        {
            if (dest.Length < GetSize()) throw new IndexOutOfRangeException();

            var header = Header;
            header.version = 1;
            header.numRanges = (ushort)Ranges.Count;
            Header = header;
            Unsafe.WriteUnaligned(ref dest[0], Header);

            var destRanges =
                MemoryMarshal.Cast<byte, TtfTableGaspRangeData>(dest.Slice(Unsafe.SizeOf<TtfTableGaspHeaderData>()));

            for (var i = 0; i < Ranges.Count; i++) destRanges[i] = Ranges[i];
        }

        public override void Deserialize(DeserializationContext context, ref BufferReader data)
        {
            data.ReadUnaligned(out TtfTableGaspHeaderData header);
            Header = header;

            Ranges.Clear();
            Ranges.Capacity = header.numRanges;

            for (var i = 0; i < header.numRanges; i++)
            {
                data.ReadUnaligned(out TtfTableGaspRangeData range);
                Ranges.Add(range);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableGaspHeaderData
    {
        public U16 version; // 1
        public U16 numRanges;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableGaspRangeData
    {
        public U16 rangeMaxPPEM;
        private U16 rangeGaspBehaviour;

        public TtfTableGaspRangeBehaviour RangeGaspBehaviour
        {
            get => (TtfTableGaspRangeBehaviour)(ushort)rangeGaspBehaviour;
            set => rangeGaspBehaviour = (ushort)value;
        }
    }

    [Flags]
    public enum TtfTableGaspRangeBehaviour : ushort
    {
        Gridfit = 1 << 0,
        Dogray = 1 << 1,
        SymmetricGridfit = 1 << 2,
        SymmetricSmoothing = 1 << 3
    }
}