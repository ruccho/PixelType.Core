using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PixelType.TrueType;

namespace PixelType
{
    public class TtfTableEbdt : TrueTypeTable
    {
        public override uint Tag => TrueTypeFont.ToTableTag("EBDT");

        public override Type[] DeserializationDependencies { get; } =
        {
            typeof(TtfTableEblc)
        };

        private TtfTableEbdtHeaderData Header { get; set; }

        public List<ITtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmap>> BitmapSections { get; } = new();

        public override void Validate(ValidationContext context)
        {
            var header = Header;
            header.majorVersion = 2;
            header.minorVersion = 0;


            Header = header;
        }

        public override long GetSize()
        {
            return Unsafe.SizeOf<TtfTableEbdtHeaderData>() +
                   BitmapSections.Sum(s => s.Bitmaps.Sum(b => b.GetSize()));
        }

        public override void Serialize(Span<byte> dest)
        {
            if (dest.Length < GetSize()) throw new ArgumentOutOfRangeException();

            Unsafe.WriteUnaligned(ref dest[0], Header);

            var bitmaps = dest.Slice(Unsafe.SizeOf<TtfTableEbdtHeaderData>());

            var cursor = 0;

            foreach (var section in BitmapSections)
            foreach (var bitmap in section.Bitmaps)
            {
                var size = (int)bitmap.GetSize();
                bitmap.Serialize(bitmaps.Slice(cursor, size));
                cursor += size;
            }
        }

        public override void Deserialize(DeserializationContext context, ref BufferReader data)
        {
            data.ReadUnaligned(out TtfTableEbdtHeaderData header);
            Header = header;

            if (header.majorVersion != 2 || header.minorVersion != 0)
                throw new NotSupportedException(
                    $"Unsupported EBDT table version {header.majorVersion}.{header.minorVersion}");

            var eblc = context.DeserializedTables.OfType<TtfTableEblc>().First();

            BitmapSections.Clear();
            foreach (var size in eblc.Sizes)
            foreach (var subtable in size.Subtables)
            {
                var indexFormat = (ushort)subtable.SubtableHeader.indexFormat;
                var imageFormat = (ushort)subtable.SubtableHeader.imageFormat;
                var imageDataOffset = (int)(uint)subtable.SubtableHeader.imageDataOffset;

                var imageDataReader = data.SliceFromStart(imageDataOffset);
                var numGlyphs = subtable.NumGlyphs;

                if (subtable is TtfTableEblcIndexSubtableMonospace monospace)
                {
                    // index format 2 or 5 (for monospace)
                    // image format 5 only
                    if (imageFormat != 5)
                        throw new NotSupportedException(
                            $"The combination of index subtable format {indexFormat} and image format {imageFormat} is not supported.");

                    var imageSize = (int)monospace.ImageSize;
                    var metrics = monospace.Metrics;

                    var section = new TtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmap5>();
                    for (var i = 0; i < numGlyphs; i++)
                    {
                        // bit aligned bitmap
                        var bitmap = new TtfTableEbdtGlyphBitmap5();
                        bitmap.Deserialize(ref imageDataReader, metrics, imageSize);
                        section.Bitmaps.Add(bitmap);
                    }

                    monospace.SectionTyped = section;
                    BitmapSections.Add(section);
                }
                else if (subtable is TtfTableEblcIndexSubtableVariableMetrics variableMetrics)
                {
                    // other
                    if (imageFormat == 5)
                        throw new NotSupportedException(
                            $"The combination of index subtable format {indexFormat} and image format {imageFormat} is not supported.");

                    ITtfTableEbdtGlyphBitmapSection<T> Process<T>(ref BufferReader imageDataReader, Func<T> creator)
                        where T : TtfTableEbdtGlyphBitmapVariableMetrics
                    {
                        var section = new TtfTableEbdtGlyphBitmapSection<T>();

                        var offsetCount = variableMetrics.OffsetCount;
                        if (offsetCount < numGlyphs + 1)
                            throw new InvalidOperationException("offset count must be greater than numGlyphs + 1.");
                        if (offsetCount > 0)
                        {
                            var offsetCursor = (int)variableMetrics.GetOffset(0);

                            for (var i = 0; i < numGlyphs; i++)
                            {
                                var bitmap = creator();
                                var next = (int)variableMetrics.GetOffset(i + 1);
                                var length = next - offsetCursor;

                                var glyphReader = imageDataReader.SliceFromStart(offsetCursor, length);
                                bitmap.Deserialize(ref glyphReader);
                                section.Bitmaps.Add(bitmap);

                                offsetCursor = next;
                            }
                        }

                        return section;
                    }

                    ITtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmapVariableMetrics> section = imageFormat switch
                    {
                        1 => Process(ref imageDataReader, () => new TtfTableEbdtGlyphBitmap1()),
                        2 => Process(ref imageDataReader, () => new TtfTableEbdtGlyphBitmap2()),
                        6 => Process(ref imageDataReader, () => new TtfTableEbdtGlyphBitmap6()),
                        7 => Process(ref imageDataReader, () => new TtfTableEbdtGlyphBitmap7()),
                        _ => throw new NotSupportedException($"Unsupported bitmap format {imageFormat}")
                    };

                    variableMetrics.SectionTyped = section;
                    BitmapSections.Add(section);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableEbdtHeaderData
    {
        // currently always 2
        public U16 majorVersion;

        // currently always 0
        public U16 minorVersion;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableEbdtBigGlyphMetricsData
    {
        public U8 height;
        public U8 width;
        public I8 horiBearingX;
        public I8 horiBearingY;
        public U8 horiAdvance;
        public I8 vertBearingX;
        public I8 vertBearingY;
        public U8 vertAdvance;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableEbdtSmallGlyphMetricsData
    {
        public U8 height;
        public U8 width;
        public I8 bearingX;
        public I8 bearingY;
        public U8 advance;
    }

    public interface ITtfTableEbdtGlyphBitmapSection<out T> where T : TtfTableEbdtGlyphBitmap
    {
        IReadOnlyList<T> Bitmaps { get; }
    }

    public class TtfTableEbdtGlyphBitmapSection<T> : ITtfTableEbdtGlyphBitmapSection<T>
        where T : TtfTableEbdtGlyphBitmap
    {
        public List<T> Bitmaps { get; } = new();
        IReadOnlyList<T> ITtfTableEbdtGlyphBitmapSection<T>.Bitmaps => Bitmaps;
    }

    public abstract class TtfTableEbdtGlyphBitmap
    {
        protected static void DeserializeBitmapByteAlignedByLine(byte[,] result, ref BufferReader reader)
        {
            var width = result.GetLength(0);
            var height = result.GetLength(1);

            // byte aligned
            for (var y = 0; y < height; y++)
            {
                var bitCursor = 0;
                byte current = 0;

                for (var x = 0; x < width; x++)
                {
                    result[x, y] = (byte)(current & 0b1000_0000);

                    current <<= 1;
                    bitCursor++;

                    // bitCursor mod 8 == 0
                    if ((bitCursor & 0b111) == 0) reader.ReadUnaligned(out current);
                }
            }
        }

        protected static void DeserializeBitmapBitAligned(byte[,] result, ref BufferReader reader)
        {
            var width = result.GetLength(0);
            var height = result.GetLength(1);

            var bitCursor = 0;
            byte current = 0;

            // bit aligned
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                // bitCursor mod 8 == 0
                if ((bitCursor & 0b111) == 0) reader.ReadUnaligned(out current);

                result[x, y] = (byte)(current & 0b1000_0000);

                current <<= 1;
                bitCursor++;
            }
        }

        protected static long GetSizeOfBitAlignedBitmap(int width, int height)
        {
            return ((width * height + 7) >> 3) << 3;
        }

        protected static long GetSizeOfByteAlignedByLineBitmap(int width, int height)
        {
            return (((width + 7) >> 3) << 3) * height;
        }

        protected static void SerializeBitAlignedBitmap(byte[,] bitmap, Span<byte> dest)
        {
            var width = bitmap.GetLength(0);
            var height = bitmap.GetLength(1);

            var bitCursor = 0;
            byte current = 0;

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                current <<= 1;

                if (bitmap[x, y] > 0) current |= 0b1;

                if ((bitCursor & 0b111) == 0b111) dest[bitCursor >> 3] = current;
                bitCursor++;
            }

            // mod
            if ((bitCursor & 0b111) != 0)
            {
                do
                {
                    current <<= 1;
                    bitCursor++;
                } while ((bitCursor & 0b111) != 0);

                dest[(bitCursor >> 3) - 1] = current;
            }
        }

        protected static void SerializeByteAlignedByLineBitmap(byte[,] bitmap, Span<byte> dest)
        {
            var width = bitmap.GetLength(0);
            var height = bitmap.GetLength(1);

            var bitCursor = 0;

            for (var y = 0; y < height; y++)
            {
                byte current = 0;
                for (var x = 0; x < width; x++)
                {
                    current <<= 1;
                    if (bitmap[x, y] > 0) current |= 0b1;

                    if ((bitCursor & 0b111) == 0b111) dest[bitCursor >> 3] = current;
                    bitCursor++;
                }

                // mod
                if ((bitCursor & 0b111) != 0)
                {
                    do
                    {
                        current <<= 1;
                        bitCursor++;
                    } while ((bitCursor & 0b111) != 0);

                    dest[(bitCursor >> 3) - 1] = current;
                }
            }
        }

        public abstract long GetSize();
        public abstract void Serialize(Span<byte> dest);
    }

    public abstract class TtfTableEbdtGlyphBitmapVariableMetrics : TtfTableEbdtGlyphBitmap
    {
        public abstract void Deserialize(ref BufferReader reader);
    }

    public abstract class TtfTableEbdtGlyphBitmapVariableMetricsSmall : TtfTableEbdtGlyphBitmapVariableMetrics
    {
        public TtfTableEbdtSmallGlyphMetricsData SmallMetrics { get; set; }
    }

    public abstract class TtfTableEbdtGlyphBitmapVariableMetricsBig : TtfTableEbdtGlyphBitmapVariableMetrics
    {
        public TtfTableEbdtBigGlyphMetricsData BigMetrics { get; set; }
    }

    /// <summary>
    ///     variable metrics, byte-aligned
    /// </summary>
    public class TtfTableEbdtGlyphBitmap1 : TtfTableEbdtGlyphBitmapVariableMetricsSmall
    {
        public byte[,] Bitmap { get; set; }

        public override void Deserialize(ref BufferReader reader)
        {
            reader.ReadUnaligned(out TtfTableEbdtSmallGlyphMetricsData smallMetrics);

            SmallMetrics = smallMetrics;

            var width = smallMetrics.width;
            var height = smallMetrics.height;

            Bitmap = new byte[width, height];
            DeserializeBitmapByteAlignedByLine(Bitmap, ref reader);
        }

        public override long GetSize()
        {
            return Unsafe.SizeOf<TtfTableEbdtSmallGlyphMetricsData>() +
                   GetSizeOfByteAlignedByLineBitmap(Bitmap.GetLength(0), Bitmap.GetLength(1));
        }

        public override void Serialize(Span<byte> dest)
        {
            if (dest.Length < GetSize())
                throw new ArgumentOutOfRangeException();

            Unsafe.WriteUnaligned(ref dest[0], SmallMetrics);
            SerializeByteAlignedByLineBitmap(Bitmap, dest.Slice(Unsafe.SizeOf<TtfTableEbdtSmallGlyphMetricsData>()));
        }
    }

    /// <summary>
    ///     variable metrics, bit-aligned
    /// </summary>
    public class TtfTableEbdtGlyphBitmap2 : TtfTableEbdtGlyphBitmapVariableMetricsSmall
    {
        public byte[,] Bitmap { get; set; }

        public override void Deserialize(ref BufferReader reader)
        {
            reader.ReadUnaligned(out TtfTableEbdtSmallGlyphMetricsData smallMetrics);

            SmallMetrics = smallMetrics;

            var width = smallMetrics.width;
            var height = smallMetrics.height;

            Bitmap = new byte[width, height];
            DeserializeBitmapBitAligned(Bitmap, ref reader);
        }

        public override long GetSize()
        {
            return Unsafe.SizeOf<TtfTableEbdtSmallGlyphMetricsData>() +
                   GetSizeOfBitAlignedBitmap(Bitmap.GetLength(0), Bitmap.GetLength(1));
        }

        public override void Serialize(Span<byte> dest)
        {
            if (dest.Length < GetSize())
                throw new ArgumentOutOfRangeException();

            Unsafe.WriteUnaligned(ref dest[0], SmallMetrics);
            SerializeBitAlignedBitmap(Bitmap, dest.Slice(Unsafe.SizeOf<TtfTableEbdtSmallGlyphMetricsData>()));
        }
    }

    /// <summary>
    ///     monospace, bit-aligned
    /// </summary>
    public class TtfTableEbdtGlyphBitmap5 : TtfTableEbdtGlyphBitmap
    {
        public byte[,] Bitmap { get; set; }

        public void Deserialize(ref BufferReader reader, in TtfTableEbdtBigGlyphMetricsData metrics, int imageSize)
        {
            var width = (byte)metrics.width;
            var height = (byte)metrics.height;

            Bitmap = new byte[width, height];

            var bytes = reader.ReadBytes(imageSize);

            var bitCursor = 0;
            var current = 0;

            // bit aligned
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                // bitCursor mod 8 == 0
                if ((bitCursor & 0b111) == 0) current = bytes[bitCursor >> 3];

                Bitmap[x, y] = (byte)(current >> 7);

                current <<= 1;
                bitCursor++;
            }
        }

        public override long GetSize()
        {
            return GetSizeOfBitAlignedBitmap(Bitmap.GetLength(0), Bitmap.GetLength(1));
        }

        public override void Serialize(Span<byte> dest)
        {
            SerializeBitAlignedBitmap(Bitmap, dest);
        }
    }

    /// <summary>
    ///     variable metrics, byte-aligned, vertical
    /// </summary>
    public class TtfTableEbdtGlyphBitmap6 : TtfTableEbdtGlyphBitmapVariableMetricsBig
    {
        public byte[,] Bitmap { get; set; }

        public override void Deserialize(ref BufferReader reader)
        {
            reader.ReadUnaligned(out TtfTableEbdtBigGlyphMetricsData smallMetrics);

            BigMetrics = smallMetrics;

            var width = smallMetrics.width;
            var height = smallMetrics.height;

            Bitmap = new byte[width, height];

            DeserializeBitmapByteAlignedByLine(Bitmap, ref reader);
        }

        public override long GetSize()
        {
            return Unsafe.SizeOf<TtfTableEbdtBigGlyphMetricsData>() +
                   GetSizeOfByteAlignedByLineBitmap(Bitmap.GetLength(0), Bitmap.GetLength(1));
        }

        public override void Serialize(Span<byte> dest)
        {
            if (dest.Length < GetSize())
                throw new ArgumentOutOfRangeException();

            Unsafe.WriteUnaligned(ref dest[0], BigMetrics);
            SerializeByteAlignedByLineBitmap(Bitmap, dest.Slice(Unsafe.SizeOf<TtfTableEbdtBigGlyphMetricsData>()));
        }
    }

    /// <summary>
    ///     varible metrics, bit-aligned, vertical
    /// </summary>
    public class TtfTableEbdtGlyphBitmap7 : TtfTableEbdtGlyphBitmapVariableMetricsBig
    {
        public byte[,] Bitmap { get; set; }

        public override void Deserialize(ref BufferReader reader)
        {
            reader.ReadUnaligned(out TtfTableEbdtBigGlyphMetricsData smallMetrics);

            BigMetrics = smallMetrics;

            var width = smallMetrics.width;
            var height = smallMetrics.height;

            Bitmap = new byte[width, height];

            DeserializeBitmapBitAligned(Bitmap, ref reader);
        }

        public override long GetSize()
        {
            return Unsafe.SizeOf<TtfTableEbdtBigGlyphMetricsData>() +
                   GetSizeOfBitAlignedBitmap(Bitmap.GetLength(0), Bitmap.GetLength(1));
        }

        public override void Serialize(Span<byte> dest)
        {
            if (dest.Length < GetSize())
                throw new ArgumentOutOfRangeException();

            Unsafe.WriteUnaligned(ref dest[0], BigMetrics);
            SerializeBitAlignedBitmap(Bitmap, dest.Slice(Unsafe.SizeOf<TtfTableEbdtBigGlyphMetricsData>()));
        }
    }
}