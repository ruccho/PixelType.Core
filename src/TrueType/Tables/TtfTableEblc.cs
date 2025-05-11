using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PixelType.TrueType;

namespace PixelType
{
    public class TtfTableEblc : TrueTypeTable
    {
        public TtfTableEblcHeaderData Header { get; set; }
        public List<TtfTableEblcSize> Sizes { get; } = new();

        public override Type[] ValidationDependencies { get; } = { typeof(TtfTableEbdt) };

        public override uint Tag => TrueTypeFont.ToTableTag("EBLC");

        public override void Validate(ValidationContext context)
        {
            var ebdt = context.ValidatedTables.OfType<TtfTableEbdt>().First();

            foreach (var size in Sizes)
            {
                foreach (var subtable in size.Subtables)
                {
                    subtable.ValidateWithEbdt(ebdt);
                    subtable.ValidateGlyphs();
                }

                size.ValidateData();
            }
        }

        public override long GetSize()
        {
            return
                // TtfTableEblcHeaderData header
                Unsafe.SizeOf<TtfTableEblcHeaderData>() +
                // TtfTableEblcBitmapSizeData[] sizes
                Unsafe.SizeOf<TtfTableEblcBitmapSizeData>() * Sizes.Count +
                // [] subtablesForSizes
                Sizes.Sum(size =>
                {
                    return
                        // TtfTableEblcIndexSubtableLookupData[] IndexSubTableArray
                        Unsafe.SizeOf<TtfTableEblcIndexSubtableLookupData>() * size.Subtables.Count +
                        // TtfTableEblcIndexSubtable[]
                        size.Subtables.Sum(subtable =>
                        {
                            return
                                // TtfTableEblcIndexSubtableHeaderData header
                                Unsafe.SizeOf<TtfTableEblcIndexSubtableHeaderData>() +
                                // body
                                (((subtable.GetBodySize() + 3) >> 2) << 2); // 32-bit alignment
                        });
                });
        }

        public override void Serialize(Span<byte> dest)
        {
            if (dest.Length < GetSize()) throw new IndexOutOfRangeException();

            var header = Header;
            header.majorVersion = 2;
            header.minorVersion = 0;
            header.numSizes = (uint)Sizes.Count;
            Header = header;

            Unsafe.WriteUnaligned(ref dest[0], Header);

            var headerSize = Unsafe.SizeOf<TtfTableEblcHeaderData>();
            var sizeDataSize = Unsafe.SizeOf<TtfTableEblcBitmapSizeData>();
            var subtableLookupSize = Unsafe.SizeOf<TtfTableEblcIndexSubtableLookupData>();
            var subtableHeaderSize = Unsafe.SizeOf<TtfTableEblcIndexSubtableHeaderData>();

            var indexSubTableArrayCursor = headerSize +
                                           sizeDataSize * Sizes.Count;
            for (var i = 0; i < Sizes.Count; i++)
            {
                var size = Sizes[i];

                var additionalOffsetToIndexSubtableCursor =
                    subtableLookupSize * size.Subtables.Count;

                for (var iSubtable = 0; iSubtable < size.Subtables.Count; iSubtable++)
                {
                    var subtable = size.Subtables[iSubtable];
                    var lookup = subtable.Lookup;
                    lookup.additionalOffsetToIndexSubtable = (uint)additionalOffsetToIndexSubtableCursor;
                    subtable.Lookup = lookup;

                    Unsafe.WriteUnaligned(ref dest[indexSubTableArrayCursor + iSubtable * subtableLookupSize],
                        subtable.Lookup);

                    var subtableOffset = indexSubTableArrayCursor + additionalOffsetToIndexSubtableCursor;
                    Unsafe.WriteUnaligned(ref dest[subtableOffset], subtable.SubtableHeader);
                    var bodySize = subtable.GetBodySize();
                    var alignedBodySize = ((bodySize + 3) >> 2) << 2;

                    subtable.SerializeBody(dest.Slice(subtableOffset + subtableHeaderSize, bodySize));

                    additionalOffsetToIndexSubtableCursor += subtableHeaderSize + alignedBodySize;
                }

                var data = size.Data;
                data.indexTablesSize = (uint)additionalOffsetToIndexSubtableCursor;
                data.indexSubTableArrayOffset = (uint)indexSubTableArrayCursor;
                data.colorRef = 0;
                data.numberOfIndexSubTables = (uint)size.Subtables.Count;
                size.Data = data;
                Unsafe.WriteUnaligned(ref dest[headerSize + sizeDataSize * i], size.Data);

                indexSubTableArrayCursor += additionalOffsetToIndexSubtableCursor;
            }
        }

        public override void Deserialize(DeserializationContext context, ref BufferReader data)
        {
            data.ReadUnaligned(out TtfTableEblcHeaderData header);
            Header = header;

            if (header.majorVersion != 2 || header.minorVersion != 0)
                throw new NotSupportedException(
                    $"Unsupported EBLC table version: {header.majorVersion}.{header.minorVersion}");

            var bitmapSizes = data.ReadUnaligned<TtfTableEblcBitmapSizeData>((int)(uint)header.numSizes);

            Sizes.Clear();
            foreach (var sizeData in bitmapSizes)
            {
                var offset = (int)(uint)sizeData.indexSubTableArrayOffset;
                var lookups = data.SliceFromStart(offset);

                var size = new TtfTableEblcSize
                {
                    Data = sizeData
                };

                for (var i = 0; i < sizeData.numberOfIndexSubTables; i++)
                {
                    lookups.ReadUnaligned(out TtfTableEblcIndexSubtableLookupData lookup);
                    var subtableReader =
                        data.SliceFromStart(offset + (int)(uint)lookup.additionalOffsetToIndexSubtable);
                    subtableReader.ReadUnaligned(out TtfTableEblcIndexSubtableHeaderData subtableHeader);

                    ushort indexFormat = subtableHeader.indexFormat;
                    TtfTableEblcIndexSubtableBase subtable = indexFormat switch
                    {
                        1 => new TtfTableEblcIndexSubtable1(lookup, subtableHeader),
                        2 => new TtfTableEblcIndexSubtable2(lookup, subtableHeader),
                        3 => new TtfTableEblcIndexSubtable3(lookup, subtableHeader),
                        _ => throw new NotSupportedException($"Unsupported EBLC index subtable format: {indexFormat}")
                    };

                    subtable.DeserializeSubtableBody(ref subtableReader);

                    size.Subtables.Add(subtable);
                }

                Sizes.Add(size);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableEblcHeaderData
    {
        // currently always 2
        public U16 majorVersion;

        // curreltly always 0
        public U16 minorVersion;

        public U32 numSizes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableEblcBitmapSizeData
    {
        public U32 indexSubTableArrayOffset;
        public U32 indexTablesSize;
        public U32 numberOfIndexSubTables;
        public U32 colorRef;
        public TtfTableEblcSbitLineMetricsData hori;
        public TtfTableEblcSbitLineMetricsData vert;
        public U16 startGlyphIndex;
        public U16 endGlyphIndex;
        public U8 ppemX;
        public U8 ppemY;
        public U8 bitDepth;
        public TtfTableEblcBitmapFlags flags;
    }

    [Flags]
    public enum TtfTableEblcBitmapFlags : byte
    {
        HorizontalMetrics = 0x01,
        VerticalMetrics = 0x02
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableEblcSbitLineMetricsData
    {
        public I8 ascender;
        public I8 descender;
        public U8 widthMax;
        public I8 caretSlopeNumerator;
        public I8 caretSlopeDenominator;
        public I8 caretOffset;
        public I8 minOriginSB;
        public I8 minAdvanceSB;
        public I8 maxBeforeBL;
        public I8 minAfterBL;
        private readonly I8 pad1;
        private readonly I8 pad2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableEblcIndexSubtableLookupData
    {
        public U16 firstGlyphIndex;
        public U16 lastGlyphIndex;
        public U32 additionalOffsetToIndexSubtable;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableEblcIndexSubtableHeaderData
    {
        public U16 indexFormat;
        public U16 imageFormat;
        public U32 imageDataOffset;
    }

    public class TtfTableEblcSize
    {
        public TtfTableEblcBitmapSizeData Data { get; set; }
        public List<TtfTableEblcIndexSubtableBase> Subtables { get; } = new();

        public void ValidateData()
        {
            byte widthMax = 0;

            var anyHorizontalMetrics = false;
            var anyVerticalMetrics = false;

            var hMinOriginSB = sbyte.MaxValue;
            var hMinAdvanceSB = sbyte.MaxValue;
            var hMaxBeforeBL = sbyte.MinValue;
            var hMinAfterBL = sbyte.MaxValue;
            var vMinOriginSB = sbyte.MaxValue;
            var vMinAdvanceSB = sbyte.MaxValue;
            var vMaxBeforeBL = sbyte.MinValue;
            var vMinAfterBL = sbyte.MaxValue;

            ushort? startGlyphIndex = null;
            ushort endGlyphIndex = 0;

            foreach (var subtable in Subtables)
            {
                startGlyphIndex ??= subtable.Lookup.firstGlyphIndex;
                endGlyphIndex = subtable.Lookup.lastGlyphIndex;

                if (subtable.Section is ITtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmapVariableMetricsSmall>
                    small)
                {
                    if ((Data.flags & TtfTableEblcBitmapFlags.HorizontalMetrics) != 0)
                        foreach (var bitmap in small.Bitmaps)
                        {
                            var metrics = bitmap.SmallMetrics;
                            var width = metrics.width;
                            sbyte originSB = metrics.bearingX;
                            var advanceSB = (sbyte)(metrics.advance - (metrics.bearingX + metrics.width));
                            sbyte beforeBL = metrics.bearingY;
                            var afterBL = (sbyte)(metrics.bearingY - metrics.height);

                            if (widthMax < width) widthMax = width;
                            if (hMinOriginSB > originSB) hMinOriginSB = originSB;
                            if (hMinAdvanceSB > advanceSB) hMinAdvanceSB = advanceSB;
                            if (hMaxBeforeBL < beforeBL) hMaxBeforeBL = beforeBL;
                            if (hMinAfterBL > afterBL) hMinAfterBL = afterBL;
                            anyHorizontalMetrics = true;
                        }
                }
                else if (subtable.Section is
                         ITtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmapVariableMetricsBig> big)
                {
                    foreach (var bitmap in big.Bitmaps)
                    {
                        var metrics = bitmap.BigMetrics;
                        var width = metrics.width;

                        if (widthMax < width) widthMax = width;

                        if ((Data.flags & TtfTableEblcBitmapFlags.HorizontalMetrics) != 0)
                        {
                            sbyte originSB = metrics.horiBearingX;
                            var advanceSB = (sbyte)(metrics.horiAdvance - (metrics.horiBearingX + metrics.width));
                            sbyte beforeBL = metrics.horiBearingX;
                            var afterBL = (sbyte)(metrics.horiBearingX - metrics.height);

                            if (hMinOriginSB > originSB) hMinOriginSB = originSB;
                            if (hMinAdvanceSB > advanceSB) hMinAdvanceSB = advanceSB;
                            if (hMaxBeforeBL < beforeBL) hMaxBeforeBL = beforeBL;
                            if (hMinAfterBL > afterBL) hMinAfterBL = afterBL;
                            anyHorizontalMetrics = true;
                        }

                        if ((Data.flags & TtfTableEblcBitmapFlags.VerticalMetrics) != 0)
                        {
                            sbyte originSB = metrics.vertBearingY;
                            var advanceSB = (sbyte)(metrics.vertAdvance - (metrics.vertBearingY + metrics.height));
                            sbyte beforeBL = metrics.vertBearingX;
                            var afterBL = (sbyte)(metrics.vertBearingX - metrics.width);

                            if (vMinOriginSB > originSB) vMinOriginSB = originSB;
                            if (vMinAdvanceSB > advanceSB) vMinAdvanceSB = advanceSB;
                            if (vMaxBeforeBL < beforeBL) vMaxBeforeBL = beforeBL;
                            if (vMinAfterBL > afterBL) vMinAfterBL = afterBL;
                            anyVerticalMetrics = true;
                        }
                    }
                }
            }

            var data = Data;
            data.hori.widthMax = widthMax;
            data.vert.widthMax = widthMax;

            if ((Data.flags & TtfTableEblcBitmapFlags.HorizontalMetrics) != 0)
            {
                if (!anyHorizontalMetrics) throw new InvalidOperationException("No horizontal glyphs");
                data.hori.minOriginSB = hMinOriginSB;
                data.hori.minAdvanceSB = hMinAdvanceSB;
                data.hori.maxBeforeBL = hMaxBeforeBL;
                data.hori.minAfterBL = hMinAfterBL;
            }

            if ((Data.flags & TtfTableEblcBitmapFlags.VerticalMetrics) != 0)
            {
                if (!anyVerticalMetrics) throw new InvalidOperationException("No vertical glyphs");
                data.vert.minOriginSB = vMinOriginSB;
                data.vert.minAdvanceSB = vMinAdvanceSB;
                data.vert.maxBeforeBL = vMaxBeforeBL;
                data.vert.minAfterBL = vMinAfterBL;
            }

            if (!startGlyphIndex.HasValue) throw new InvalidOperationException("No glyph in a size");

            data.startGlyphIndex = startGlyphIndex.Value;
            data.endGlyphIndex = endGlyphIndex;

            Data = data;
        }
    }

    public abstract class TtfTableEblcIndexSubtableBase
    {
        protected TtfTableEblcIndexSubtableBase(TtfTableEblcIndexSubtableLookupData lookup,
            TtfTableEblcIndexSubtableHeaderData subtableHeader)
        {
            Lookup = lookup;
            SubtableHeader = subtableHeader;
        }

        public abstract ITtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmap> Section { get; }

        public TtfTableEblcIndexSubtableLookupData Lookup { get; set; }
        public TtfTableEblcIndexSubtableHeaderData SubtableHeader { get; set; }

        public virtual int NumGlyphs => Lookup.lastGlyphIndex - Lookup.firstGlyphIndex + 1;

        protected abstract ushort IndexFormat { get; }

        public abstract void ValidateGlyphs();

        public void ValidateWithEbdt(TtfTableEbdt ebdt)
        {
            var numGlyphs = Lookup.lastGlyphIndex - Lookup.firstGlyphIndex + 1;
            if (numGlyphs != Section.Bitmaps.Count)
                throw new IndexOutOfRangeException(
                    "number of glyphs doesn't match with specified bitmap section.");

            var header = SubtableHeader;

            header.indexFormat = IndexFormat;
            header.imageFormat = Section switch
            {
                ITtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmap1> => 1,
                ITtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmap2> => 2,
                ITtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmap5> => 5,
                ITtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmap6> => 6,
                ITtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmap7> => 7,
                _ => throw new NotSupportedException("Unsupported bitmap format")
            };

            var offset = Unsafe.SizeOf<TtfTableEbdtHeaderData>();
            var resolved = false;
            foreach (var section in ebdt.BitmapSections)
            {
                if (section == Section)
                {
                    header.imageDataOffset = (uint)offset;
                    resolved = true;
                    break;
                }

                offset += (int)section.Bitmaps.Sum(b => b.GetSize());
            }

            if (!resolved) throw new InvalidOperationException("this section is not included in EBDT table.");

            SubtableHeader = header;
        }

        public abstract void DeserializeSubtableBody(ref BufferReader bodyReader);
        public abstract int GetBodySize();
        public abstract void SerializeBody(Span<byte> dest);
    }

    public abstract class TtfTableEblcIndexSubtable<T> : TtfTableEblcIndexSubtableBase where T : TtfTableEbdtGlyphBitmap
    {
        protected TtfTableEblcIndexSubtable(TtfTableEblcIndexSubtableLookupData lookup,
            TtfTableEblcIndexSubtableHeaderData subtableHeader) : base(lookup, subtableHeader)
        {
        }

        public override ITtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmap> Section => SectionTyped;
        public ITtfTableEbdtGlyphBitmapSection<T> SectionTyped { get; set; }

        public override void ValidateGlyphs()
        {
        }
    }

    public abstract class TtfTableEblcIndexSubtableMonospace : TtfTableEblcIndexSubtable<TtfTableEbdtGlyphBitmap5>
    {
        protected TtfTableEblcIndexSubtableMonospace(TtfTableEblcIndexSubtableLookupData lookup,
            TtfTableEblcIndexSubtableHeaderData subtableHeader) : base(lookup, subtableHeader)
        {
        }

        public abstract TtfTableEbdtBigGlyphMetricsData Metrics { get; }
        public abstract uint ImageSize { get; }
    }

    public abstract class
        TtfTableEblcIndexSubtableVariableMetrics : TtfTableEblcIndexSubtable<TtfTableEbdtGlyphBitmapVariableMetrics>
    {
        protected TtfTableEblcIndexSubtableVariableMetrics(TtfTableEblcIndexSubtableLookupData lookup,
            TtfTableEblcIndexSubtableHeaderData subtableHeader) : base(lookup, subtableHeader)
        {
        }

        public abstract int OffsetCount { get; }
        public abstract uint GetOffset(int index);
    }

    public class TtfTableEblcIndexSubtable1 : TtfTableEblcIndexSubtableVariableMetrics
    {
        public TtfTableEblcIndexSubtable1(TtfTableEblcIndexSubtableLookupData lookup,
            TtfTableEblcIndexSubtableHeaderData subtableHeader) : base(lookup, subtableHeader)
        {
        }

        public List<U32> SbitOffsets { get; } = new();

        protected override ushort IndexFormat => 1;

        public override int OffsetCount => SbitOffsets.Count;

        public override void DeserializeSubtableBody(ref BufferReader bodyReader)
        {
            SbitOffsets.Clear();
            SbitOffsets.AddRange(bodyReader.ReadUnaligned<U32>(NumGlyphs + 1).ToArray());
        }

        public override void ValidateGlyphs()
        {
            base.ValidateGlyphs();
            SbitOffsets.Clear();
            SbitOffsets.Capacity = SectionTyped.Bitmaps.Count + 1;
            uint cursor = 0;
            foreach (var bitmap in SectionTyped.Bitmaps)
            {
                SbitOffsets.Add(cursor);
                cursor += (uint)bitmap.GetSize();
            }

            SbitOffsets.Add(cursor);
        }

        public override int GetBodySize()
        {
            return Unsafe.SizeOf<U32>() * SbitOffsets.Count;
        }

        public override void SerializeBody(Span<byte> dest)
        {
            var destAsU32 = MemoryMarshal.Cast<byte, U32>(dest);
            for (var i = 0; i < SbitOffsets.Count; i++) destAsU32[i] = SbitOffsets[i];
        }

        public override uint GetOffset(int index)
        {
            return SbitOffsets[index];
        }
    }

    public class TtfTableEblcIndexSubtable2 : TtfTableEblcIndexSubtableMonospace
    {
        private TtfTableEbdtBigGlyphMetricsData bigMetrics;

        private uint imageSize;


        public TtfTableEblcIndexSubtable2(TtfTableEblcIndexSubtableLookupData lookup,
            TtfTableEblcIndexSubtableHeaderData subtableHeader) : base(lookup, subtableHeader)
        {
        }

        public override TtfTableEbdtBigGlyphMetricsData Metrics => bigMetrics;
        public override uint ImageSize => imageSize;

        protected override ushort IndexFormat => 2;

        public override void DeserializeSubtableBody(ref BufferReader bodyReader)
        {
            bodyReader.ReadUnaligned(out U32 rawImageSize);
            imageSize = rawImageSize;
            bodyReader.ReadUnaligned(out bigMetrics);
        }

        public override int GetBodySize()
        {
            return Unsafe.SizeOf<U32>() + Unsafe.SizeOf<TtfTableEbdtBigGlyphMetricsData>();
        }

        public override void SerializeBody(Span<byte> dest)
        {
            if (dest.Length < GetBodySize()) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref dest[0], (U32)imageSize);
            Unsafe.WriteUnaligned(ref dest[Unsafe.SizeOf<U32>()], Metrics);
        }
    }

    public class TtfTableEblcIndexSubtable3 : TtfTableEblcIndexSubtableVariableMetrics
    {
        public TtfTableEblcIndexSubtable3(TtfTableEblcIndexSubtableLookupData lookup,
            TtfTableEblcIndexSubtableHeaderData subtableHeader) : base(lookup, subtableHeader)
        {
        }

        private List<U16> SbitOffsets { get; } = new();

        protected override ushort IndexFormat => 3;

        public override int OffsetCount => SbitOffsets.Count;

        public override void DeserializeSubtableBody(ref BufferReader bodyReader)
        {
            SbitOffsets.Clear();
            SbitOffsets.AddRange(bodyReader.ReadUnaligned<U16>(NumGlyphs + 1).ToArray());
        }

        public override int GetBodySize()
        {
            return Unsafe.SizeOf<U16>() * SbitOffsets.Count;
        }

        public override void SerializeBody(Span<byte> dest)
        {
            var destAsU16 = MemoryMarshal.Cast<byte, U16>(dest);
            for (var i = 0; i < SbitOffsets.Count; i++) destAsU16[i] = SbitOffsets[i];
        }

        public override uint GetOffset(int index)
        {
            return SbitOffsets[index];
        }
    }
}