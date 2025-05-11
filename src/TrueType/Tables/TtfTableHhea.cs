using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace PixelType.TrueType
{
    // Horizontal Header
    public class TtfTableHhea : TrueTypeTableFixed<TtfTableHheaData>
    {
        public override uint Tag => TrueTypeFont.ToTableTag("hhea");
        public override Type[] ValidationDependencies { get; } = { typeof(TtfTableHmtx) };

        public override void Validate(ValidationContext context)
        {
            var hmtx = context.ValidatedTables.OfType<TtfTableHmtx>().First();

            var data = Data;
            data.numOfLongHorMetrics = (ushort)hmtx.LongHorMetric.Count;

            ushort advanceWidthMax = 0;
            foreach (var m in hmtx.LongHorMetric)
                if (advanceWidthMax < m.advanceWidth)
                    advanceWidthMax = m.advanceWidth;

            data.advanceWidthMax = advanceWidthMax;

            Data = data;
        }

        public override void Serialize(Span<byte> dest)
        {
            var data = Data;
            data.version = Fixed.FromInt32(0x00010000);
            data.reserved0 = 0;
            data.reserved1 = 0;
            data.reserved2 = 0;
            data.reserved3 = 0;
            data.metricDataFormat = 0;
            Data = data;

            base.Serialize(dest);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableHheaData
    {
        public Fixed version;
        public FWord ascent;
        public FWord descent;
        public FWord lineGap;
        public UFWord advanceWidthMax;
        public FWord minLeftSideBearing;
        public FWord minRightSideBearing;
        public FWord xMaxExtent;
        public I16 caretSlopeRise;
        public I16 caretSlopeRun;
        public FWord caretOffset;
        public I16 reserved0;
        public I16 reserved1;
        public I16 reserved2;
        public I16 reserved3;
        public I16 metricDataFormat;
        public U16 numOfLongHorMetrics;
    }
}