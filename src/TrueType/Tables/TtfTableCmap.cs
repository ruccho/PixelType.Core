using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PixelType.TrueType
{
    public class TtfTableCmap : TrueTypeTable
    {
        private readonly HashSet<TtfTableCmapSubtable> tempTables = new();
        public override uint Tag => TrueTypeFont.ToTableTag("cmap");

        public List<TtfTableCmapEncodingSubtable> EncodingSubtables { get; } = new();

        public override long GetSize()
        {
            tempTables.Clear();
            foreach (var et in EncodingSubtables) tempTables.Add(et.Subtable);

            var numEncodingTables = EncodingSubtables.Count;
            var numTables = tempTables.Count;

            long sumSubtableSize = 0;
            foreach (var t in tempTables) sumSubtableSize += t.GetSize();

            return Marshal.SizeOf<TtfTableCmapHeaderData>() + // header
                   numEncodingTables * Marshal.SizeOf<TtfTableCmapEncodingData>() + // encoding subtables
                   sumSubtableSize;
        }

        public override void Serialize(Span<byte> dest)
        {
            if (dest.Length != GetSize()) throw new ArgumentException();

            EncodingSubtables.Sort((a, b) =>
            {
                var platformId = a.PlatformId - b.PlatformId;
                if (platformId != 0) return platformId;
                return a.PlatformSpecificId - b.PlatformSpecificId;
            });

            var numEncodingTables = EncodingSubtables.Count;

            var header = new TtfTableCmapHeaderData
            {
                version = 0,
                numberSubTables = (ushort)numEncodingTables
            };

            Utils.Serialize(header, dest, 0, out var headerSize);

            tempTables.Clear();
            foreach (var et in EncodingSubtables) tempTables.Add(et.Subtable);

            var sizeOfEncodingSubtable = Marshal.SizeOf<TtfTableCmapEncodingData>();
            var subtableOffset = (uint)(headerSize +
                                        numEncodingTables * sizeOfEncodingSubtable);

            var subtables = new Dictionary<TtfTableCmapSubtable, uint>();

            foreach (var t in tempTables)
            {
                subtables[t] = subtableOffset;
                var size = (uint)t.GetSize();
                t.Serialize(dest.Slice((int)subtableOffset, (int)size));
                subtableOffset += size;
            }

            for (var i = 0; i < EncodingSubtables.Count; i++)
            {
                var t = EncodingSubtables[i];
                var et = new TtfTableCmapEncodingData
                {
                    platformId = t.PlatformId,
                    platformSpecificId = t.PlatformSpecificId,
                    offset = subtables[t.Subtable]
                };

                Utils.Serialize(et, dest, headerSize + i * sizeOfEncodingSubtable);
            }
        }

        public override void Deserialize(DeserializationContext context, ref BufferReader data)
        {
            // var header = Utils.Deserialize<TtfTableCmapHeaderData>(data, 0);
            data.ReadUnaligned(out TtfTableCmapHeaderData header);

            if (header.version != 0) throw new NotSupportedException();

            EncodingSubtables.Clear();

            // var subtableData = data.Slice(Marshal.SizeOf<TtfTableCmapHeaderData>());

            var subtables = new Dictionary<uint, TtfTableCmapSubtable>();

            // int sizeOfEncodingSubtable = Marshal.SizeOf<TtfTableCmapEncodingSubtableData>();
            for (var i = 0; i < header.numberSubTables; i++)
            {
                data.ReadUnaligned(out TtfTableCmapEncodingData encoding);
                // var encodingSubtable = Utils.Deserialize<TtfTableCmapEncodingSubtableData>(subtableData, i * sizeOfEncodingSubtable);

                uint offset = encoding.offset;
                if (!subtables.TryGetValue(offset, out var subtable))
                {
                    var encodingSubtable = data.SliceFromStart((int)(uint)encoding.offset);
                    encodingSubtable.ReadUnaligned(out U16 format);

                    /*
                    var subtableHeadSliceU16 =
                        MemoryMarshal.Cast<byte, U16>(data.Slice((int)(uint)encodingSubtable.offset, 4));
                    var subtableHeadSliceU32 =
                        MemoryMarshal.Cast<byte, U32>(data.Slice((int)(uint)encodingSubtable.offset, 8));
                        */
                    // ushort format = subtableHeadSliceU16[0];

                    uint subtableSize;
                    switch ((ushort)format)
                    {
                        case 0:
                        {
                            subtable = new TtfTableCmapSubtable0();
                            encodingSubtable.ReadUnaligned(out U16 size);
                            subtableSize = size;
                            break;
                        }
                        case 4:
                        {
                            subtable = new TtfTableCmapSubtable4();
                            encodingSubtable.ReadUnaligned(out U16 size);
                            subtableSize = size;
                            break;
                        }
                        case 6:
                        {
                            subtable = new TtfTableCmapSubtable6();
                            encodingSubtable.ReadUnaligned(out U16 size);
                            subtableSize = size;
                            break;
                        }
                        case 12:
                        {
                            subtable = new TtfTableCmapSubtable12();
                            encodingSubtable.ReadUnaligned<U16>(out _);
                            encodingSubtable.ReadUnaligned(out U32 size);
                            subtableSize = size;
                            break;
                        }
                        default:
                            //TODO: Implement other formats
                            throw new NotSupportedException($"Unsupported cmap format \"{format}\"");
                    }

                    var subtableSlice = data.SliceFromStart((int)(uint)encoding.offset, (int)subtableSize);
                    subtable.Deserialize(ref subtableSlice);

                    subtables[offset] = subtable;
                }

                EncodingSubtables.Add(new TtfTableCmapEncodingSubtable(encoding, subtable));
            }
        }
    }

    public class TtfTableCmapEncodingSubtable
    {
        public TtfTableCmapEncodingSubtable(TtfTableCmapEncodingData data,
            TtfTableCmapSubtable subtable)
        {
            PlatformId = data.platformId;
            PlatformSpecificId = data.platformSpecificId;
            Subtable = subtable;
        }

        public TtfTableCmapEncodingSubtable(ushort platformId, ushort platformSpecificId,
            TtfTableCmapSubtable subtable)
        {
            PlatformId = platformId;
            PlatformSpecificId = platformSpecificId;
            Subtable = subtable;
        }

        public ushort PlatformId { get; set; }
        public ushort PlatformSpecificId { get; set; }
        public TtfTableCmapSubtable Subtable { get; set; }
    }

    public abstract class TtfTableCmapSubtable
    {
        public abstract long GetSize();
        public abstract void Serialize(Span<byte> dest);
        public abstract void Deserialize(ref BufferReader data);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableCmapHeaderData
    {
        public U16 version;
        public U16 numberSubTables;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableCmapEncodingData
    {
        public U16 platformId;
        public U16 platformSpecificId;
        public U32 offset;
    }

    public class TtfTableCmapSubtable0 : TtfTableCmapSubtable
    {
        public TtfTableCmapSubtable0HeaderData Header { get; set; }
        public U8[] GlyphIndexArray { get; } = new U8[256];

        public override long GetSize()
        {
            return 262;
        }

        public override void Serialize(Span<byte> dest)
        {
            if (dest.Length != GetSize()) throw new ArgumentException();

            var header = Header;
            header.format = 0;
            header.length = 262;
            Header = header;

            Utils.Serialize(header, dest, 0, out var headerSize);

            var arraySlice = MemoryMarshal.Cast<byte, U8>(dest.Slice(headerSize));

            GlyphIndexArray.CopyTo(arraySlice);
        }

        public override void Deserialize(ref BufferReader data)
        {
            if (data.Remains != 262) throw new ArgumentException();

            data.ReadUnaligned(out TtfTableCmapSubtable0HeaderData header);
            Header = header;

            data.ReadBytes(MemoryMarshal.Cast<U8, byte>(GlyphIndexArray));
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableCmapSubtable0HeaderData
    {
        public U16 format;
        public U16 length;
        public U16 language;
    }

    public class TtfTableCmapSubtable4 : TtfTableCmapSubtable
    {
        public TtfTableCmapSubtable4HeaderData Header { get; set; }

        public List<TtfTableCmapSubtable4Segment> Segments { get; } = new();

        public List<ushort> GlyphIndexArray { get; } = new();

        public override long GetSize()
        {
            var headerSize = Marshal.SizeOf<TtfTableCmapSubtable4HeaderData>();
            var segCount = Segments.Count;
            var glyphIndexArraySize = GlyphIndexArray.Count * sizeof(ushort);

            return headerSize +
                   segCount * sizeof(ushort) * 4 + // four attributes
                   sizeof(ushort) + // reservedPad 
                   glyphIndexArraySize;
        }

        public override void Serialize(Span<byte> dest)
        {
            var size = (int)GetSize();
            if (size != dest.Length) throw new ArgumentException();

            var segCount = Segments.Count;

            if (segCount == 0) throw new InvalidOperationException();

            int msb;
            {
                var p = (ushort)segCount;
                int i;
                for (i = 0; i < sizeof(ushort) * 8; i++)
                {
                    if (p == 0) break;
                    p >>= 1;
                }

                msb = i - 1;
            }

            var header = Header;
            header.format = 4;
            header.length = (ushort)size;
            header.segCountX2 = (ushort)(segCount * 2);
            header.searchRange = (ushort)(1 << (msb + 1));
            header.entrySelector = (ushort)msb;
            header.rangeShift = (ushort)(2 * segCount - header.searchRange);

            Header = header;

            Utils.Serialize(header, dest, 0, out var headerSize);
            var dataSlice = MemoryMarshal.Cast<byte, U16>(dest.Slice(headerSize, dest.Length - headerSize));

            var cursor = 0;

            var endCode = dataSlice.Slice(cursor, segCount);
            cursor += segCount + 1;

            var startCode = dataSlice.Slice(cursor, segCount);
            cursor += segCount;

            var idDelta = dataSlice.Slice(cursor, segCount);
            cursor += segCount;

            var idRangeOffset = dataSlice.Slice(cursor, segCount);
            cursor += segCount;

            for (var i = 0; i < segCount; i++)
            {
                var seg = Segments[i];

                startCode[i] = seg.startCode;
                endCode[i] = seg.endCode;
                idDelta[i] = seg.idDelta;
                idRangeOffset[i] = seg.idRangeOffset;
            }

            if (GlyphIndexArray.Count > 0)
            {
                var glyphIndexArray = dataSlice.Slice(cursor);

                var tempGlyphIndexArray = GlyphIndexArray.ToArray();

                for (var i = 0; i < glyphIndexArray.Length; i++) glyphIndexArray[i] = tempGlyphIndexArray[i];
            }
        }

        public override void Deserialize(ref BufferReader data)
        {
            data.ReadUnaligned(out TtfTableCmapSubtable4HeaderData header);
            Header = header;

            int segCountX2 = Header.segCountX2;
            var segCount = segCountX2 / 2;

            var endCodes = data.ReadBuffer(segCountX2);
            data.Skip(Unsafe.SizeOf<U16>()); // reservedPad

            var startCodes = data.ReadBuffer(segCountX2);

            var idDeltas = data.ReadBuffer(segCountX2);

            var idRangeOffsets = data.ReadBuffer(segCountX2);

            Segments.Clear();
            for (var i = 0; i < segCount; i++)
            {
                startCodes.ReadUnaligned(out U16 startCode);
                endCodes.ReadUnaligned(out U16 endCode);
                idDeltas.ReadUnaligned(out U16 idDelta);
                idRangeOffsets.ReadUnaligned(out U16 idRangeOffset);
                Segments.Add(new TtfTableCmapSubtable4Segment
                {
                    startCode = startCode,
                    endCode = endCode,
                    idDelta = idDelta,
                    idRangeOffset = idRangeOffset
                });
            }

            GlyphIndexArray.Clear();
            if (data.Remains > 0)
            {
                var numIndices = data.Remains / 2;
                var tempGlyphIndexArray = new ushort[numIndices];
                for (var i = 0; i < numIndices; i++)
                {
                    data.ReadUnaligned(out U16 index);
                    tempGlyphIndexArray[i] = index;
                }

                GlyphIndexArray.AddRange(tempGlyphIndexArray);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableCmapSubtable4HeaderData
    {
        public U16 format;
        public U16 length;
        public U16 language;
        public U16 segCountX2;
        public U16 searchRange;
        public U16 entrySelector;
        public U16 rangeShift;
    }

    public struct TtfTableCmapSubtable4Segment
    {
        public ushort startCode;
        public ushort endCode;
        public ushort idDelta;
        public ushort idRangeOffset;
    }

    public class TtfTableCmapSubtable6 : TtfTableCmapSubtable
    {
        public TtfTableCmapSubtable6HeaderData Header { get; set; }
        public U16[] GlyphIndexArray { get; set; } = Array.Empty<U16>();

        public override long GetSize()
        {
            return Marshal.SizeOf<TtfTableCmapSubtable6HeaderData>() + GlyphIndexArray.Length * sizeof(ushort);
        }

        public override void Serialize(Span<byte> dest)
        {
            var size = GetSize();
            if (dest.Length != size) throw new ArgumentException();

            var header = Header;
            header.length = (ushort)size;
            Header = header;

            Utils.Serialize(header, dest, 0, out var headerSize);

            var arraySlice = MemoryMarshal.Cast<byte, U16>(dest.Slice(headerSize));

            GlyphIndexArray.CopyTo(arraySlice);
        }

        public override void Deserialize(ref BufferReader data)
        {
            var wholeLength = data.Remains;
            data.ReadUnaligned(out TtfTableCmapSubtable6HeaderData header);
            Header = header;

            if (Header.length != wholeLength) throw new ArgumentException();

            var arraySlice = data.ReadUnalignedAsPossible<U16>();

            if (Header.entryCount != arraySlice.Length) throw new ArgumentException();

            GlyphIndexArray = arraySlice.ToArray();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableCmapSubtable6HeaderData
    {
        public U16 format;
        public U16 length;
        public U16 language;
        public U16 firstCode;
        public U16 entryCount;
    }

    public class TtfTableCmapSubtable12 : TtfTableCmapSubtable
    {
        public TtfTableCmapSubtable12HeaderData Header { get; set; }
        public List<TtfTableCmapSubtable12MapGroup> Groups { get; } = new();

        public override long GetSize()
        {
            return Marshal.SizeOf<TtfTableCmapSubtable12HeaderData>() +
                   Marshal.SizeOf<TtfTableCmapSubtable12MapGroup>() * Groups.Count;
        }

        public override void Serialize(Span<byte> dest)
        {
            var size = GetSize();
            if (size != dest.Length) throw new ArgumentException();

            var header = Header;
            header.format = 12;
            header.reserved = 0;
            header.length = (uint)size;
            header.numGroups = (uint)Groups.Count;
            Header = header;

            Utils.Serialize(header, dest, 0, out var headerSize);
            var groupSize = Marshal.SizeOf<TtfTableCmapSubtable12MapGroup>();
            for (var i = 0; i < Groups.Count; i++)
            {
                var g = Groups[i];
                Utils.Serialize(g, dest, headerSize + i * groupSize);
            }
        }

        public override void Deserialize(ref BufferReader data)
        {
            data.ReadUnaligned(out TtfTableCmapSubtable12HeaderData header);
            Header = header;

            // int groupSize = Marshal.SizeOf<TtfTableCmapSubtable12MapGroup>();

            Groups.Clear();

            for (var i = 0; i < Header.numGroups; i++)
            {
                data.ReadUnaligned(out TtfTableCmapSubtable12MapGroup group);
                Groups.Add(group);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableCmapSubtable12HeaderData
    {
        public U16 format;
        public U16 reserved;
        public U32 length;
        public U32 language;
        public U32 numGroups;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableCmapSubtable12MapGroup
    {
        public U32 startCharCode;
        public U32 endCharCode;
        public U32 startGlyphCode;
    }
}