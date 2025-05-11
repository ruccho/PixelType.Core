using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PixelType.TrueType
{
    public class TtfTableOs2 : TrueTypeTable
    {
        private static readonly int[] codePages =
        {
            1252,
            1250,
            1251,
            1253,
            1254,
            1255,
            1256,
            1257,
            1258,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            874,
            932,
            936,
            949,
            950,
            1361,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            869,
            866,
            865,
            864,
            863,
            862,
            861,
            860,
            857,
            855,
            852,
            775,
            737,
            708,
            850,
            437
        };

        private static readonly UnicodeRangeMapping[] unicodeRangeMappings =
        {
            new(0x0000, 0x007F, 0),
            new(0x0080, 0x00FF, 1),
            new(0x0100, 0x017F, 2),
            new(0x0180, 0x024F, 3),
            new(0x0250, 0x02AF, 4),
            new(0x1D00, 0x1D7F, 4),
            new(0x1D80, 0x1DBF, 4),
            new(0x02B0, 0x02FF, 5),
            new(0xA700, 0xA71F, 5),
            new(0x0300, 0x036F, 6),
            new(0x1DC0, 0x1DFF, 6),
            new(0x0370, 0x03FF, 7),
            new(0x2C80, 0x2CFF, 8),
            new(0x0400, 0x04FF, 9),
            new(0x0500, 0x052F, 9),
            new(0x2DE0, 0x2DFF, 9),
            new(0xA640, 0xA69F, 9),
            new(0x0530, 0x058F, 10),
            new(0x0590, 0x05FF, 11),
            new(0xA500, 0xA63F, 12),
            new(0x0600, 0x06FF, 13),
            new(0x0750, 0x077F, 13),
            new(0x07C0, 0x07FF, 14),
            new(0x0900, 0x097F, 15),
            new(0x0980, 0x09FF, 16),
            new(0x0A00, 0x0A7F, 17),
            new(0x0A80, 0x0AFF, 18),
            new(0x0B00, 0x0B7F, 19),
            new(0x0B80, 0x0BFF, 20),
            new(0x0C00, 0x0C7F, 21),
            new(0x0C80, 0x0CFF, 22),
            new(0x0D00, 0x0D7F, 23),
            new(0x0E00, 0x0E7F, 24),
            new(0x0E80, 0x0EFF, 25),
            new(0x10A0, 0x10FF, 26),
            new(0x2D00, 0x2D2F, 26),
            new(0x1B00, 0x1B7F, 27),
            new(0x1100, 0x11FF, 28),
            new(0x1E00, 0x1EFF, 29),
            new(0x2C60, 0x2C7F, 29),
            new(0xA720, 0xA7FF, 29),
            new(0x1F00, 0x1FFF, 30),
            new(0x2000, 0x206F, 31),
            new(0x2E00, 0x2E7F, 31),
            new(0x2070, 0x209F, 32),
            new(0x20A0, 0x20CF, 33),
            new(0x20D0, 0x20FF, 34),
            new(0x2100, 0x214F, 35),
            new(0x2150, 0x218F, 36),
            new(0x2190, 0x21FF, 37),
            new(0x27F0, 0x27FF, 37),
            new(0x2900, 0x297F, 37),
            new(0x2B00, 0x2BFF, 37),
            new(0x2200, 0x22FF, 38),
            new(0x2A00, 0x2AFF, 38),
            new(0x27C0, 0x27EF, 38),
            new(0x2980, 0x29FF, 38),
            new(0x2300, 0x23FF, 39),
            new(0x2400, 0x243F, 40),
            new(0x2440, 0x245F, 41),
            new(0x2460, 0x24FF, 42),
            new(0x2500, 0x257F, 43),
            new(0x2580, 0x259F, 44),
            new(0x25A0, 0x25FF, 45),
            new(0x2600, 0x26FF, 46),
            new(0x2700, 0x27BF, 47),
            new(0x3000, 0x303F, 48),
            new(0x3040, 0x309F, 49),
            new(0x30A0, 0x30FF, 50),
            new(0x31F0, 0x31FF, 50),
            new(0x3100, 0x312F, 51),
            new(0x31A0, 0x31BF, 51),
            new(0x3130, 0x318F, 52),
            new(0xA840, 0xA87F, 53),
            new(0x3200, 0x32FF, 54),
            new(0x3300, 0x33FF, 55),
            new(0xAC00, 0xD7AF, 56),
            new(0x10000, 0x10FFFF, 57),
            new(0x10900, 0x1091F, 58),
            new(0x4E00, 0x9FFF, 59),
            new(0x2E80, 0x2EFF, 59),
            new(0x2F00, 0x2FDF, 59),
            new(0x2FF0, 0x2FFF, 59),
            new(0x3400, 0x4DBF, 59),
            new(0x20000, 0x2A6DF, 59),
            new(0x3190, 0x319F, 59),
            new(0xE000, 0xF8FF, 60),
            new(0x31C0, 0x31EF, 61),
            new(0xF900, 0xFAFF, 61),
            new(0x2F800, 0x2FA1F, 61),
            new(0xFB00, 0xFB4F, 62),
            new(0xFB50, 0xFDFF, 63),
            new(0xFE20, 0xFE2F, 64),
            new(0xFE10, 0xFE1F, 65),
            new(0xFE30, 0xFE4F, 65),
            new(0xFE50, 0xFE6F, 66),
            new(0xFE70, 0xFEFF, 67),
            new(0xFF00, 0xFFEF, 68),
            new(0xFFF0, 0xFFFF, 69),
            new(0x0F00, 0x0FFF, 70),
            new(0x0700, 0x074F, 71),
            new(0x0780, 0x07BF, 72),
            new(0x0D80, 0x0DFF, 73),
            new(0x1000, 0x109F, 74),
            new(0x1200, 0x137F, 75),
            new(0x1380, 0x139F, 71),
            new(0x2D80, 0x2DDF, 71),
            new(0x13A0, 0x13FF, 76),
            new(0x1400, 0x167F, 77),
            new(0x1680, 0x169F, 78),
            new(0x16A0, 0x16FF, 79),
            new(0x1780, 0x17FF, 80),
            new(0x19E0, 0x19FF, 80),
            new(0x1800, 0x18AF, 81),
            new(0x2800, 0x28FF, 82),
            new(0xA000, 0xA48F, 83),
            new(0xA490, 0xA4CF, 83),
            new(0x1700, 0x171F, 84),
            new(0x1720, 0x173F, 84),
            new(0x1740, 0x175F, 84),
            new(0x1760, 0x177F, 84),
            new(0x10300, 0x1032F, 85),
            new(0x10330, 0x1034F, 86),
            new(0x10400, 0x1044F, 87),
            new(0x1D000, 0x1D0FF, 88),
            new(0x1D100, 0x1D1FF, 88),
            new(0x1D200, 0x1D24F, 88),
            new(0x1D400, 0x1D7FF, 89),
            new(0xF0000, 0xFFFFD, 90),
            new(0x100000, 0x10FFFD, 90),
            new(0xFE00, 0xFE0F, 91),
            new(0xE0100, 0xE01EF, 91),
            new(0xE0000, 0xE007F, 92),
            new(0x1900, 0x194F, 93),
            new(0x1950, 0x197F, 94),
            new(0x1980, 0x19DF, 95),
            new(0x1A00, 0x1A1F, 96),
            new(0x2C00, 0x2C5F, 97),
            new(0x2D30, 0x2D7F, 98),
            new(0x4DC0, 0x4DFF, 99),
            new(0xA800, 0xA82F, 100),
            new(0x10000, 0x1007F, 101),
            new(0x10080, 0x100FF, 101),
            new(0x10100, 0x1013F, 101),
            new(0x10140, 0x1018F, 102),
            new(0x10380, 0x1039F, 103),
            new(0x103A0, 0x103DF, 104),
            new(0x10450, 0x1047F, 105),
            new(0x10480, 0x104AF, 106),
            new(0x10800, 0x1083F, 107),
            new(0x10A00, 0x10A5F, 108),
            new(0x1D300, 0x1D35F, 109),
            new(0x12000, 0x123FF, 110),
            new(0x12400, 0x1247F, 110),
            new(0x1D360, 0x1D37F, 111),
            new(0x1B80, 0x1BBF, 112),
            new(0x1C00, 0x1C4F, 113),
            new(0x1C50, 0x1C7F, 114),
            new(0xA880, 0xA8DF, 115),
            new(0xA900, 0xA92F, 116),
            new(0xA930, 0xA95F, 117),
            new(0xAA00, 0xAA5F, 118),
            new(0x10190, 0x101CF, 119),
            new(0x101D0, 0x101FF, 120),
            new(0x102A0, 0x102DF, 121),
            new(0x10280, 0x1029F, 121),
            new(0x10920, 0x1093F, 121),
            new(0x1F030, 0x1F09F, 122),
            new(0x1F000, 0x1F02F, 122)
        };

        public TtfTableOs2DataV0 v0;
        public TtfTableOs2DataV1Appendix v1apdx;
        public TtfTableOs2DataV2Appendix v2apdx;
        public TtfTableOs2DataV5Appendix v5apdx;

        static TtfTableOs2()
        {
            Array.Sort(unicodeRangeMappings, (a, b) => (int)(b.range.from - a.range.from));
        }

        public TtfTableOs2()
        {
            v0.version = 1;
            v0.usWeightClass = 400;
            v0.usWidthClass = 5;
            /*
            v0.fsType = 0;
            v0.sFamilyClass = 0;
            v0.panose0_bFamilyType = 0;
            v0.panose1_bSerifType = 0;
            v0.panose2_bWeight = 0;
            v0.panose3_bProportion = 0;
            v0.panose4_bContrast = 0;
            v0.panose5_bStrokeVariation = 0;
            v0.panose6_bArmStyle = 0;
            v0.panose7_bLetterform = 0;
            v0.panose8_bMidline = 0;
            v0.panose9_bXHeight = 0;
            */
        }

        public override uint Tag => TrueTypeFont.ToTableTag("OS/2");

        public override Type[] ValidationDependencies { get; } =
        {
            typeof(TtfTableGlyf), typeof(TtfTableHhea), typeof(TtfTableHead), typeof(TtfTablePost), typeof(TtfTableCmap)
        };

        public ushort Version => v0.version;


        public override void Validate(ValidationContext context)
        {
            var glyf = context.ValidatedTables.OfType<TtfTableGlyf>().First();
            var hhea = context.ValidatedTables.OfType<TtfTableHhea>().First();
            var head = context.ValidatedTables.OfType<TtfTableHead>().First();
            var post = context.ValidatedTables.OfType<TtfTablePost>().First();
            var cmap = context.ValidatedTables.OfType<TtfTableCmap>().First();

            double avgWidth = 0;
            var glyphs = glyf.Entries.OfType<TtfTableGlyfEntrySimple>();
            var numGlyphs = glyphs.Count();
            var invNumGlyphs = 1.0 / numGlyphs;
            foreach (var g in glyphs) avgWidth += (g.Header.xMax - g.Header.xMin) * invNumGlyphs;

            v0.xAvgCharWidth = (short)avgWidth;
            var miniSizeX = (short)(hhea.Data.advanceWidthMax / 2);
            var miniSizeY = (short)((head.Data.yMax - head.Data.yMin) / 2);
            v0.ySubscriptXSize = miniSizeX;
            v0.ySubscriptYSize = miniSizeY;
            v0.ySubscriptXOffset = 0;
            v0.ySubscriptYOffset = miniSizeY;

            v0.ySuperscriptXSize = miniSizeX;
            v0.ySuperscriptYSize = miniSizeY;
            v0.ySuperscriptXOffset = 0;
            v0.ySuperscriptYOffset = miniSizeY;

            v0.yStrikeoutSize = (short)post.Header.underlineThickness;
            v0.yStrikeoutPosition = (short)(head.Data.yMax / 2);

            var cmapEncodingTable =
                cmap.EncodingSubtables.FirstOrDefault(s => s.PlatformId == 0 && s.Subtable is TtfTableCmapSubtable4);

            if (cmapEncodingTable == null)
                throw new NotSupportedException(
                    "To validate OS/2 table, cmap table must contain subtable format 4 with platform ID equals 0.");
            var cmapTable = cmapEncodingTable.Subtable as TtfTableCmapSubtable4;

            var rangeMappingCursor = 0;
            v0.ulUnicodeRange1 = 0;
            v0.ulUnicodeRange2 = 0;
            v0.ulUnicodeRange3 = 0;
            v0.ulUnicodeRange4 = 0;
            var firstChar = ushort.MaxValue;
            ushort lastChar = 0;
            foreach (var seg in cmapTable.Segments)
            {
                var segStart = seg.startCode;
                var segEnd = seg.endCode;

                var segEndValid = segEnd <= 0xFFFD ? segEnd : (ushort)0xFFFD;

                if (segStart < firstChar) firstChar = segStart;
                if (segStart <= segEndValid && lastChar < segEndValid) lastChar = segEndValid;

                for (; rangeMappingCursor < unicodeRangeMappings.Length; rangeMappingCursor++)
                {
                    var m = unicodeRangeMappings[rangeMappingCursor];
                    if (segEnd < m.range.from) break;
                    if (m.range.to < segStart) continue;

                    if (m.bit < 32) v0.ulUnicodeRange1 |= (uint)(1 << (31 - m.bit));
                    else if (m.bit < 64) v0.ulUnicodeRange2 |= (uint)(1 << (63 - m.bit));
                    else if (m.bit < 96) v0.ulUnicodeRange3 |= (uint)(1 << (95 - m.bit));
                    else if (m.bit < 128) v0.ulUnicodeRange4 |= (uint)(1 << (127 - m.bit));
                    else throw new IndexOutOfRangeException();
                }

                rangeMappingCursor--;
                if (rangeMappingCursor < 0) rangeMappingCursor = 0;
            }

            v0.usFirstCharIndex = firstChar;
            v0.usLastCharIndex = lastChar;
            v0.sTypoAscender = (short)hhea.Data.ascent;
            v0.sTypoDescender = (short)hhea.Data.descent;
            v0.sTypoLineGap = (short)hhea.Data.lineGap;

            v0.usWinAscent = (ushort)(short)hhea.Data.ascent;
            v0.usWinDescent = (ushort)-hhea.Data.descent;

            // Microsoft code pages

            var windowsUnicodeBmp =
                cmap.EncodingSubtables.FirstOrDefault(s => s.PlatformId == 3 && s.PlatformSpecificId == 1);

            if (windowsUnicodeBmp != null)
            {
                if (windowsUnicodeBmp.Subtable is not TtfTableCmapSubtable4 subtable)
                    throw new InvalidOperationException("Windows Unicode BMP mapping subtable must be format 4.");

                Span<byte> buffer = stackalloc byte[16];
                char source = default;
                char temp = default;
                var sourceSpan = MemoryMarshal.CreateSpan(ref source, 1);
                var destSpan = MemoryMarshal.CreateSpan(ref temp, 1);
                for (var i = 0; i < codePages.Length; i++)
                {
                    var codePage = codePages[i];
                    if (codePage == 0) continue;
                    var encoding = CodePagesEncodingProvider.Instance.GetEncoding(codePage);

                    var matched = false;
                    foreach (var seg in subtable.Segments)
                    {
                        for (var c = seg.startCode; c < seg.endCode; c++)
                        {
                            // ignore characters contained in ASCII
                            if (c < 128) continue;

                            source = (char)c;

                            // char (unicode) -> codepage
                            var count = encoding.GetByteCount(sourceSpan);
                            if (buffer.Length < count) continue;

                            count = encoding.GetBytes(sourceSpan, buffer);

                            // codepage -> char (unicode)
                            var bytes = buffer.Slice(0, count);
                            count = encoding.GetCharCount(bytes);
                            if (count != 1) continue;

                            count = encoding.GetChars(bytes, destSpan);
                            if (count != 1) continue;

                            if (source == temp)
                            {
                                matched = true;
                                break;
                            }
                        }

                        if (matched) break;
                    }

                    if (matched)
                    {
                        if (i < 32)
                            v1apdx.ulCodePageRange1 |= (uint)(1 << i);
                        else if (i < 64) v1apdx.ulCodePageRange2 |= (uint)(1 << (i - 32));
                    }
                }
            }
        }

        public override long GetSize()
        {
            long sum = Marshal.SizeOf<TtfTableOs2DataV0>();
            if (v0.version >= 1) sum += Marshal.SizeOf<TtfTableOs2DataV1Appendix>();
            if (v0.version >= 2) sum += Marshal.SizeOf<TtfTableOs2DataV2Appendix>();
            if (v0.version >= 5) sum += Marshal.SizeOf<TtfTableOs2DataV5Appendix>();
            return sum;
        }

        public override void Serialize(Span<byte> dest)
        {
            if (GetSize() != dest.Length) throw new ArgumentException();

            Utils.Serialize(v0, dest, 0, out var segize);
            var cursor = segize;

            if (v0.version >= 1)
            {
                Utils.Serialize(v1apdx, dest, cursor, out segize);
                cursor += segize;
            }

            if (v0.version >= 2)
            {
                Utils.Serialize(v2apdx, dest, cursor, out segize);
                cursor += segize;
            }

            if (v0.version >= 5)
            {
                Utils.Serialize(v5apdx, dest, cursor, out segize);
                cursor += segize;
            }
        }

        public override void Deserialize(DeserializationContext context, ref BufferReader data)
        {
            data.ReadUnaligned(out v0);

            if (v0.version >= 1) data.ReadUnaligned(out v1apdx);

            if (v0.version >= 2) data.ReadUnaligned(out v2apdx);

            if (v0.version >= 5) data.ReadUnaligned(out v5apdx);
        }

        private struct UnicodeRange
        {
            public uint from;
            public uint to;
        }

        private struct UnicodeRangeMapping
        {
            public readonly UnicodeRange range;
            public readonly byte bit;

            public UnicodeRangeMapping(uint from, uint to, byte bit)
            {
                range = new UnicodeRange
                {
                    from = from,
                    to = to
                };

                this.bit = bit;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableOs2DataV0
    {
        public U16 version;

        public I16 xAvgCharWidth;
        public U16 usWeightClass;
        public U16 usWidthClass;
        public U16 fsType;

        public I16 ySubscriptXSize;
        public I16 ySubscriptYSize;
        public I16 ySubscriptXOffset;
        public I16 ySubscriptYOffset;
        public I16 ySuperscriptXSize;
        public I16 ySuperscriptYSize;
        public I16 ySuperscriptXOffset;
        public I16 ySuperscriptYOffset;
        public I16 yStrikeoutSize;
        public I16 yStrikeoutPosition;

        public I16 sFamilyClass;

        public U8 panose0_bFamilyType;
        public U8 panose1_bSerifType;
        public U8 panose2_bWeight;
        public U8 panose3_bProportion;
        public U8 panose4_bContrast;
        public U8 panose5_bStrokeVariation;
        public U8 panose6_bArmStyle;
        public U8 panose7_bLetterform;
        public U8 panose8_bMidline;
        public U8 panose9_bXHeight;

        public U32 ulUnicodeRange1;
        public U32 ulUnicodeRange2;
        public U32 ulUnicodeRange3;
        public U32 ulUnicodeRange4;

        public U32 achVendId;

        public U16 fsSelection;
        public U16 usFirstCharIndex;
        public U16 usLastCharIndex;

        public I16 sTypoAscender;
        public I16 sTypoDescender;
        public I16 sTypoLineGap;

        public U16 usWinAscent;
        public U16 usWinDescent;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableOs2DataV1Appendix
    {
        public U32 ulCodePageRange1;
        public U32 ulCodePageRange2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableOs2DataV2Appendix
    {
        public I16 sxHeight;
        public I16 sCapHeight;

        public U16 usDefaultChar;
        public U16 usBreakChar;
        public U16 usMaxContext;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableOs2DataV5Appendix
    {
        public U16 usLowerOpticalPointSize;
        public U16 usUpperOpticalPointSize;
    }
}