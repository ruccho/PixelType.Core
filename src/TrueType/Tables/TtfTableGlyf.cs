using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PixelType.TrueType
{
    public class TtfTableGlyf : TrueTypeTable
    {
        public override Type[] DeserializationDependencies { get; } = { typeof(TtfTableLoca) };
        public override uint Tag => TrueTypeFont.ToTableTag("glyf");

        public bool Align { get; set; } = true;

        public List<TtfTableGlyfEntry> Entries { get; } = new();

        public override long GetSize()
        {
            long sum = 0;
            foreach (var e in Entries)
            {
                if (e == null) continue;
                var size = e.GetSize();

                if (Align) size = (size + 3) / 4 * 4;
                sum += size;
            }

            return sum;
        }

        public override void Serialize(Span<byte> dest)
        {
            long cursor = 0;
            foreach (var e in Entries)
            {
                if (e == null) continue;

                var size = e.GetSize();

                e.Serialize(dest.Slice((int)cursor, (int)size));

                if (Align) size = (size + 3) / 4 * 4;

                cursor += size;
            }
        }

        public override void Deserialize(DeserializationContext context, ref BufferReader data)
        {
            var loca = context.DeserializedTables.OfType<TtfTableLoca>().First();

            var isLongMode = loca.FormatMode == TtfTableLoca.FormatModeType.Long;

            var prevOffset = loca.Offsets[0];
            for (var i = 1; i < loca.Offsets.Count; i++)
            {
                var toLoca = loca.Offsets[i];

                var from = prevOffset;
                var to = isLongMode ? toLoca : toLoca * 2;
                prevOffset = to;

                if (from == to)
                {
                    Entries.Add(null);
                }
                else
                {
                    var slice = data.SliceFromStart((int)from, (int)(to - from));

                    Entries.Add(TtfTableGlyfEntry.Create(ref slice));
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableGlyfHeaderData
    {
        public I16 numberOfContours;
        public FWord xMin;
        public FWord yMin;
        public FWord xMax;
        public FWord yMax;
    }

    public abstract class TtfTableGlyfEntry
    {
        protected TtfTableGlyfEntry(TtfTableGlyfHeaderData header)
        {
            Header = header;
        }

        public TtfTableGlyfHeaderData Header { get; set; }

        public long GetSize()
        {
            return Marshal.SizeOf<TtfTableGlyfHeaderData>() + GetGlyphDataSize();
        }

        protected abstract long GetGlyphDataSize();

        protected abstract void SerializeGlyphData(Span<byte> dest);

        protected abstract void DeserializeGlyph(ref BufferReader data);

        public static TtfTableGlyfEntry Create(ref BufferReader data)
        {
            data.ReadUnaligned(out TtfTableGlyfHeaderData header);

            TtfTableGlyfEntry result;
            if (header.numberOfContours >= 0) result = new TtfTableGlyfEntrySimple(header);
            else throw new NotImplementedException();
            result.DeserializeGlyph(ref data);

            return result;
        }


        public void Serialize(Span<byte> dest)
        {
            if (GetSize() != dest.Length) throw new ArgumentException();

            var headerSize = Marshal.SizeOf<TtfTableGlyfHeaderData>();

            SerializeGlyphData(dest.Slice(headerSize));

            Utils.Serialize(Header, dest, 0, out _);
        }
    }

    public class TtfTableGlyfEntrySimple : TtfTableGlyfEntry
    {
        public TtfTableGlyfEntrySimple(TtfTableGlyfHeaderData header) : base(header)
        {
        }

        public List<byte> Instructions { get; set; } = new(0);
        public List<TtfTableGlyfSimpleGlyphPoint> Points { get; } = new(0);
        public List<ushort> EndPtsOfContours { get; } = new(0);

        protected override long GetGlyphDataSize()
        {
            var endPtsOfContoursSize = sizeof(ushort) * EndPtsOfContours.Count;
            var instructionSize = 2 + Instructions.Count;

            // TODO: support repeat in serialization
            var flagsSize = Points.Count;
            var coordsSize = 0;
            foreach (var p in Points)
            {
                if (p.x.IsValueOnArray) coordsSize += p.x.ShortVector ? 1 : 2;
                if (p.y.IsValueOnArray) coordsSize += p.y.ShortVector ? 1 : 2;
            }

            var sum = endPtsOfContoursSize + instructionSize + flagsSize + coordsSize;

            return sum;
        }

        protected override void SerializeGlyphData(Span<byte> dest)
        {
            if (GetGlyphDataSize() != dest.Length) throw new ArgumentException();

            //end
            var n = EndPtsOfContours.Count;

            var numPoints = n == 0 ? 0 : EndPtsOfContours[^1] + 1;
            if (numPoints != Points.Count) throw new InvalidOperationException();

            var h = Header;
            h.numberOfContours = (short)n;
            Header = h;

            var endOfContoursSize = sizeof(ushort) * n;
            var endPtsOfContoursSlice = MemoryMarshal.Cast<byte, U16>(dest.Slice(0, endOfContoursSize));

            for (var i = 0; i < EndPtsOfContours.Count; i++) endPtsOfContoursSlice[i] = EndPtsOfContours[i];

            //instruction
            var instructionLength = Instructions.Count;
            MemoryMarshal.Cast<byte, U16>(dest.Slice(endOfContoursSize, sizeof(ushort)))[0] = (ushort)instructionLength;
            var instructionsSlice = dest.Slice(endOfContoursSize + 2, instructionLength);

            Instructions.ToArray().CopyTo(instructionsSlice);

            //flags
            var flagsSlice = dest.Slice(endOfContoursSize + 2 + instructionLength);

            for (var i = 0; i < Points.Count; i++)
            {
                var p = Points[i];
                flagsSlice[i] = p.GetPreferredFlagValue();
            }

            //x
            var coordsSlice = flagsSlice.Slice(Points.Count);
            var coordsCursor = 0;
            for (var i = 0; i < Points.Count; i++)
            {
                var p = Points[i];

                if (!p.x.IsValueOnArray) continue;

                var coordValue = p.x.RawCoordValue;
                if (p.x.ShortVector)
                {
                    coordsSlice.Slice(coordsCursor, 1)[0] = (byte)coordValue;
                    coordsCursor += 1;
                }
                else
                {
                    MemoryMarshal.Cast<byte, I16>(coordsSlice.Slice(coordsCursor, 2))[0] = coordValue;
                    coordsCursor += 2;
                }
            }

            for (var i = 0; i < Points.Count; i++)
            {
                var p = Points[i];

                if (!p.y.IsValueOnArray) continue;

                var coordValue = p.y.RawCoordValue;
                if (p.y.ShortVector)
                {
                    coordsSlice.Slice(coordsCursor, 1)[0] = (byte)coordValue;
                    coordsCursor += 1;
                }
                else
                {
                    MemoryMarshal.Cast<byte, I16>(coordsSlice.Slice(coordsCursor, 2))[0] = coordValue;
                    coordsCursor += 2;
                }
            }
        }

        protected override void DeserializeGlyph(ref BufferReader data)
        {
            int n = Header.numberOfContours;

            EndPtsOfContours.Clear();
            EndPtsOfContours.Capacity = n;
            var endPtsOfContours = data.ReadUnaligned<U16>(n);
            for (var i = 0; i < n; i++) EndPtsOfContours.Add(endPtsOfContours[i]);

            data.ReadUnaligned(out U16 instructionLengthRaw);
            int instructionLength = instructionLengthRaw;
            var instructionsSlice = data.ReadBytes(instructionLength);
            Instructions.Clear();
            Instructions.Capacity = instructionLength;
            Instructions.AddRange(instructionsSlice.ToArray());

            var numPoints = EndPtsOfContours[^1] + 1;


            var points = new TtfTableGlyfSimpleGlyphPoint[numPoints];
            var pointCursor = 0;

            //flags
            //int flagCursor = 0;
            {
                while (pointCursor < numPoints)
                {
                    data.ReadUnaligned(out byte flag);
                    /*
                    byte flag = flagsSlice[flagCursor];
                    flagCursor++;
                    */

                    var point = new TtfTableGlyfSimpleGlyphPoint(flag);
                    points[pointCursor++] = point;
                    if (pointCursor >= numPoints) break;

                    var repeat = (flag & (1 << 3)) > 0;
                    if (repeat)
                    {
                        data.ReadUnaligned(out byte count);
                        /*
                        byte count = flagsSlice[flagCursor];
                        flagCursor++;
                        */

                        for (var j = 0; j < count && pointCursor < numPoints; j++) points[pointCursor++] = point;
                    }
                }
            }

            if (numPoints != pointCursor) throw new InvalidOperationException("Invalid number of points");

            // var coordsSlice = flagsSlice.Slice(flagCursor);

            // coords (x)
            // int coordsCursor = 0;
            for (var i = 0; i < pointCursor; i++)
            {
                var p = points[i];

                short coordValue;
                if (!p.x.IsValueOnArray)
                {
                    coordValue = 0;
                }
                else if (p.x.ShortVector)
                {
                    data.ReadUnaligned(out byte coordValueRaw);
                    coordValue = coordValueRaw;
                }
                else
                {
                    data.ReadUnaligned(out I16 coordValueRaw);
                    coordValue = coordValueRaw;
                }

                p.x.RawCoordValue = coordValue;
                points[i] = p;
            }

            // coords (y)
            for (var i = 0; i < pointCursor; i++)
            {
                var p = points[i];

                short coordValue;
                if (!p.y.IsValueOnArray)
                {
                    coordValue = 0;
                }
                else if (p.y.ShortVector)
                {
                    data.ReadUnaligned(out byte coordValueRaw);
                    coordValue = coordValueRaw;
                }
                else
                {
                    data.ReadUnaligned(out I16 coordValueRaw);
                    coordValue = coordValueRaw;
                }

                p.y.RawCoordValue = coordValue;
                points[i] = p;
            }


            Points.Clear();
            Points.Capacity = pointCursor;

            Points.AddRange(points);
        }
    }


    public struct TtfTableGlyfSimpleGlyphPoint
    {
        public struct Element
        {
            private bool shortVector;
            private bool flag;
            private short coordValue;
            private short rawCoordValue;

            public bool IsValueOnArray => shortVector || !flag;

            public short RawCoordValue
            {
                get => rawCoordValue;
                set
                {
                    if (shortVector)
                    {
                        if (value is < byte.MinValue or > byte.MaxValue) throw new ArgumentOutOfRangeException();
                        coordValue = flag ? value : (short)-value;
                    }
                    else
                    {
                        coordValue = flag ? (short)0 : value;
                    }

                    rawCoordValue = value;
                }
            }

            public short CoordValue
            {
                get => coordValue;
                set
                {
                    if (value == 0)
                    {
                        shortVector = false;
                        flag = true;
                        rawCoordValue = 0;
                    }
                    else if (value is >= -byte.MaxValue and <= byte.MaxValue)
                    {
                        shortVector = true;
                        flag = value >= 0;
                        rawCoordValue = flag ? value : (short)-value;
                    }
                    else
                    {
                        shortVector = false;
                        flag = false;
                        rawCoordValue = value;
                    }

                    coordValue = value;
                }
            }

            public Element(bool shortVector, bool flag)
            {
                this.shortVector = shortVector;
                this.flag = flag;
                rawCoordValue = 0;
                coordValue = 0;

                RawCoordValue = 0;
            }

            public bool ShortVector => shortVector;
            public bool Flag => flag;
        }

        public bool onCurve;
        public Element x;
        public Element y;

        public TtfTableGlyfSimpleGlyphPoint(byte flag)
        {
            onCurve = (flag & (1 << 0)) > 0;

            var xShortVector = (flag & (1 << 1)) > 0;
            var yShortVector = (flag & (1 << 2)) > 0;
            var xFlag = (flag & (1 << 4)) > 0;
            var yFlag = (flag & (1 << 5)) > 0;

            x = new Element(xShortVector, xFlag);
            y = new Element(yShortVector, yFlag);
        }

        public byte GetPreferredFlagValue()
        {
            return (byte)((onCurve ? 1 << 0 : 0) |
                          (x.ShortVector ? 1 << 1 : 0) |
                          (y.ShortVector ? 1 << 2 : 0) |
                          (x.Flag ? 1 << 4 : 0) |
                          (y.Flag ? 1 << 5 : 0));
        }
    }
}