using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PixelType.TrueType
{
    public class TtfTableHmtx : TrueTypeTable
    {
        public override Type[] DeserializationDependencies { get; } = { typeof(TtfTableHhea) };
        public override Type[] ValidationDependencies { get; } = { typeof(TtfTableGlyf) };
        public override uint Tag => TrueTypeFont.ToTableTag("hmtx");

        public List<TtfTableHmtxLongHorMetricData> LongHorMetric { get; } = new();

        public List<FWord> LeftSideBearing { get; } = new();

        public override void Validate(ValidationContext context)
        {
            var glyf = context.ValidatedTables.OfType<TtfTableGlyf>().First();

            var numGlyphs = glyf.Entries.Count;
            if (LongHorMetric.Count + LeftSideBearing.Count != numGlyphs) throw new InvalidOperationException();
        }

        public override long GetSize()
        {
            return LongHorMetric.Count * Marshal.SizeOf<TtfTableHmtxLongHorMetricData>() +
                   LeftSideBearing.Count * Marshal.SizeOf<FWord>();
        }

        public override void Serialize(Span<byte> dest)
        {
            var sourceMetricsBytes =
                MemoryMarshal.Cast<TtfTableHmtxLongHorMetricData, byte>(LongHorMetric.ToArray());
            sourceMetricsBytes.CopyTo(dest);

            var sourceBearingBytes =
                MemoryMarshal.Cast<FWord, byte>(LeftSideBearing.ToArray());
            sourceBearingBytes.CopyTo(dest.Slice(sourceMetricsBytes.Length));
        }

        public override void Deserialize(DeserializationContext context, ref BufferReader data)
        {
            var hhea = context.DeserializedTables.OfType<TtfTableHhea>().First();

            ushort numOfLongHorMetrics = hhea.Data.numOfLongHorMetrics;
            /*
            var longHorMetricBytes =
                data.Slice(0, numOfLongHorMetrics * Marshal.SizeOf<TtfTableHmtxLongHorMetricData>());
            var longHorMetric = MemoryMarshal.Cast<byte, TtfTableHmtxLongHorMetricData>(longHorMetricBytes);
            */
            var longHorMetric = data.ReadUnaligned<TtfTableHmtxLongHorMetricData>(numOfLongHorMetrics);

            var leftSideBearing = data.ReadUnalignedAsPossible<FWord>();
            // var leftSideBearing = MemoryMarshal.Cast<byte, FWord>(data.Slice(longHorMetricBytes.Length));

            LongHorMetric.Clear();
            LeftSideBearing.Clear();

            LongHorMetric.AddRange(longHorMetric.ToArray());
            LeftSideBearing.AddRange(leftSideBearing.ToArray());
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableHmtxLongHorMetricData
    {
        public U16 advanceWidth;
        public I16 leftSideBearing;
    }
}