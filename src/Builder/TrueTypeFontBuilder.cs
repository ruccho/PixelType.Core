using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using PixelType.TrueType;

namespace PixelType
{
    /// <summary>
    /// High-level API to build TrueType font.
    /// </summary>
    public class TrueTypeFontBuilder
    {
        private static readonly char[] psForbiddenChars = "[](){}<>/%".ToCharArray();

        private static readonly ushort[] CodesMappedToGlyphOne = { 0x00, 0x08, 0x0D, 0x1D };
        private static readonly byte[,] singleBitmap = new byte[1, 1];

        private readonly Dictionary<ushort, IBuilderGlyph> glyphs = new();
        private readonly ushort unmappedGlyphs = 2;

        private KeyValuePair<ushort, IBuilderGlyph>[] sortedGlyphs;


        public TrueTypeFontBuilder(ushort unitsPerEm, string fontFamily, string subfamily)
        {
            if (string.IsNullOrEmpty(fontFamily)) throw new NullReferenceException();
            if (string.IsNullOrEmpty(subfamily)) throw new NullReferenceException();

            UnitsPerEm = unitsPerEm;
            FontFamily = fontFamily;
            Subfamily = subfamily;
        }

        public IReadOnlyDictionary<ushort, IBuilderGlyph> Glyphs { get; }

        /// <summary>
        ///     Missing Character Glyph
        ///     > The first glyph (glyph index 0) must be the MISSING CHARACTER GLYPH. This glyph must have a visible appearance
        ///     and non-zero advance width.
        /// </summary>
        private IBuilderGlyph GlyphZero { get; set; }

        /// <summary>
        ///     Null Glyph
        ///     > The second glyph (glyph index 1) must be the NULL glyph. This glyph must have no contours and zero advance width.
        /// </summary>
        private IBuilderGlyph GlyphOne { get; } = new EmptyGlyph(0);

        private int NumGlyphs => unmappedGlyphs + glyphs.Count;

        public ushort UnitsPerEm { get; } = 1024;

        public ushort Baseline { get; set; }
        public ushort MetricWidth { get; set; }
        public ushort MetricHeight { get; set; }
        public ushort AutoTrimPadding { get; set; }
        public int PointScale { get; set; }

        public ushort Underline { get; set; }
        public ushort UnderlineThickness { get; set; }

        public string CopyrightNotice { get; set; } = "";

        public string FontFamily { get; set; }

        public string Subfamily { get; set; }

        public string MajorVersion { get; set; } = "1";
        public string MinorVersion { get; set; } = "0";

        public bool TryAddGlyph(char c, IBuilderGlyph glyph)
        {
            Span<char> cSpan = stackalloc char[1];
            cSpan[0] = c;

            var numBytes = Encoding.Unicode.GetByteCount(cSpan);
            if (numBytes > 2) return false;

            Span<byte> bytes = stackalloc byte[2];
            Encoding.Unicode.GetBytes(cSpan, bytes);

            var bmpCodePoint = MemoryMarshal.Cast<byte, ushort>(bytes)[0];

            return TryAddGlyph(bmpCodePoint, glyph);
        }

        public bool TryAddGlyph(ushort bmpCodePoint, IBuilderGlyph glyph)
        {
            // https://developer.apple.com/fonts/TrueType-Reference-Manual/RM07/appendixB.html
            // code U+0000 mapped to glyph 0 (missing). 0 is exceptional behaviour.
            if (bmpCodePoint == 0)
            {
                if (GlyphZero == null) GlyphZero = glyph;
                else return false;
                return true;
            }

            if (bmpCodePoint <= 0x08 ||
                (0x0A <= bmpCodePoint && bmpCodePoint <= 0x0C) ||
                (0x0E <= bmpCodePoint && bmpCodePoint <= 0x1F) ||
                bmpCodePoint == 0x7F)
                // forced to be mapped to glyph 0 or 1
                return false;
            if (bmpCodePoint == 0x09 ||
                bmpCodePoint == 0x0D ||
                bmpCodePoint == 0x20 ||
                bmpCodePoint == 0xA0)
                // must be mapped to glyph with no contours
                if (glyph.Contours.Any())
                    return false;

            return glyphs.TryAdd(bmpCodePoint, glyph);
        }

        public TrueTypeFont Build()
        {
            if (GlyphZero == null) GlyphZero = new EmptyGlyph();

            // add space if not yet
            glyphs.TryAdd(0x20, new EmptyGlyph(MetricWidth));

            sortedGlyphs = glyphs.OrderBy(pair => pair.Key).ToArray();

            var (glyf, hmtx) = BuildGlyfAndHmtx();

            var tables = new List<TrueTypeTable>();
            tables.Add(BuildCmap());
            tables.Add(glyf);
            tables.Add(BuildHead());
            tables.Add(BuildHhea());
            tables.Add(hmtx);
            tables.Add(BuildLoca());
            tables.Add(BuildMaxp());
            tables.Add(BuildName());
            tables.Add(BuildPost());
            tables.Add(BuildOS2());

            tables.Add(BuildGasp());

            var (eblc, ebdt) = BuildEblcAndEbdt();
            tables.Add(eblc);
            tables.Add(ebdt);

            return new TrueTypeFont
            {
                Tables = tables
            };
        }

        private TtfTableHead BuildHead()
        {
            var now = new LongDateTime(DateTime.UtcNow);
            var data = new TtfTableHeadData
            {
                fontRevision = Fixed.FromInt32(0x00010000),
                BaselineIsZero = true,
                LeftSidebearingIsZero = true,
                UseIntegerScaling = true,
                unitsPerEm = UnitsPerEm,
                created = now,
                modified = now,
                xMin = 0,
                yMin = (short)-Baseline,
                xMax = (short)MetricWidth,
                yMax = (short)(MetricHeight - Baseline),

                macStyle = 0,
                lowestRecPPEM = 0x0008,
                DirectionHint = TtfTableHeadData.DirectionHintType.StronglyLTRAndNeutrals
            };


            return new TtfTableHead
            {
                Data = data
            };
        }

        private TtfTableHhea BuildHhea()
        {
            var hhea = new TtfTableHhea();
            hhea.Data = new TtfTableHheaData
            {
                ascent = (short)(UnitsPerEm - Baseline),
                descent = (short)-Baseline,
                lineGap = 0,
                minLeftSideBearing = 0,
                minRightSideBearing = 0,
                xMaxExtent = (short)MetricWidth,
                caretSlopeRise = 1
            };
            return hhea;
        }

        private TtfTableMaxp BuildMaxp()
        {
            var maxp = new TtfTableMaxp();
            maxp.Data = new TtfTableMaxpData
            {
                maxZones = 2,
                maxTwilightPoints = 0,
                maxStorage = 1,
                maxFunctionDefs = 1,
                maxInstructionDefs = 0,
                maxStackElements = 0x40,
                maxSizeOfInstructions = 0,
                maxComponentElements = 0,
                maxComponentDepth = 0
            };
            return maxp;
        }

        private TtfTableLoca BuildLoca()
        {
            return new TtfTableLoca();
        }

        private TtfTableOs2 BuildOS2()
        {
            return new TtfTableOs2()
            {
                v1apdx = new()
                {
                    ulCodePageRange1 = 1 << 17 // CP932, make bitmap embedding work with ClearType
                }
            };
        }

        private static bool IsPostScriptAvailableChar(char c)
        {
            if (c < 0x21 || 0x7e < c) return false;
            if (psForbiddenChars.Contains(c)) return false;
            return true;
        }

        private TtfTableName BuildName()
        {
            var name = new TtfTableName();

            var copyrightNotice = CopyrightNotice;
            var fontFamily = FontFamily;
            var fontSubfamily = Subfamily;
            var subfamilyId = $"com.ruccho.pixeltype: {FontFamily}";
            var fullName = FontFamily;
            var versionOfNameTable = $"Version {MajorVersion}.{MinorVersion}";

            var postScriptNameSpan = FontFamily.ToCharArray();

            for (var i = 0; i < postScriptNameSpan.Length; i++)
            {
                var c = postScriptNameSpan[i];
                if (!IsPostScriptAvailableChar(c)) postScriptNameSpan[i] = '_';
            }

            var postScriptName = new string(postScriptNameSpan);

            var records = new TtfTableNameRecord[]
            {
                new(1, 0, 0, NameIdType.CopyrightNotice, copyrightNotice, Encoding.ASCII),
                new(1, 0, 0, NameIdType.FontFamily, fontFamily, Encoding.ASCII),
                new(1, 0, 0, NameIdType.FontSubfamily, fontSubfamily, Encoding.ASCII),
                new(1, 0, 0, NameIdType.SubfamilyId, subfamilyId, Encoding.ASCII),
                new(1, 0, 0, NameIdType.FullName, fullName, Encoding.ASCII),
                new(1, 0, 0, NameIdType.VersionOfNameTable, versionOfNameTable, Encoding.ASCII),
                new(1, 0, 0, NameIdType.PostScriptName, postScriptName, Encoding.ASCII),
                new(3, 1, 0x0409, NameIdType.CopyrightNotice, copyrightNotice, Encoding.BigEndianUnicode),
                new(3, 1, 0x0409, NameIdType.FontFamily, fontFamily, Encoding.BigEndianUnicode),
                new(3, 1, 0x0409, NameIdType.FontSubfamily, fontSubfamily, Encoding.BigEndianUnicode),
                new(3, 1, 0x0409, NameIdType.SubfamilyId, subfamilyId, Encoding.BigEndianUnicode),
                new(3, 1, 0x0409, NameIdType.FullName, fullName, Encoding.BigEndianUnicode),
                new(3, 1, 0x0409, NameIdType.VersionOfNameTable, versionOfNameTable, Encoding.BigEndianUnicode),
                new(3, 1, 0x0409, NameIdType.PostScriptName, postScriptName, Encoding.BigEndianUnicode)
            };

            name.Records.AddRange(records);

            return name;
        }

        private TtfTablePost BuildPost()
        {
            var post = new TtfTablePost();
            post.Header = new TtfTablePostHeaderData
            {
                format = Fixed.FromInt32(0x00030000),
                italicAngle = Fixed.FromInt32(0),
                underlinePosition = (short)(Underline - Baseline),
                underlineThickness = (short)UnderlineThickness,
                isFixedPitch = 1
            };
            return post;
        }

        private TtfTableCmap BuildCmap()
        {
            // unicode
            var unicode = new TtfTableCmapSubtable4
            {
                Header = new TtfTableCmapSubtable4HeaderData
                {
                    language = 0
                }
            };

            // insert codes to be mapped to glyph 1

            var glyphIndexMap =
                new (ushort code, ushort glyphIndex)[sortedGlyphs.Length + CodesMappedToGlyphOne.Length];
            {
                ushort sortedglyphIndex = 0;
                ushort code;
                ushort codeIndex = 0;
                for (var j = 0; j < CodesMappedToGlyphOne.Length; j++)
                {
                    var oneCode = CodesMappedToGlyphOne[j];
                    while (sortedglyphIndex < sortedGlyphs.Length &&
                           (code = sortedGlyphs[sortedglyphIndex].Key) < oneCode)
                    {
                        glyphIndexMap[codeIndex] = (code, (ushort)(unmappedGlyphs + sortedglyphIndex));
                        sortedglyphIndex++;
                        codeIndex++;
                    }

                    glyphIndexMap[codeIndex] = (oneCode, 0x01);
                    codeIndex++;
                }

                for (; sortedglyphIndex < sortedGlyphs.Length; sortedglyphIndex++)
                {
                    code = sortedGlyphs[sortedglyphIndex].Key;
                    glyphIndexMap[codeIndex] = (code, (ushort)(unmappedGlyphs + sortedglyphIndex));
                    codeIndex++;
                }

                if (codeIndex != glyphIndexMap.Length) throw new InvalidOperationException();
            }

            // construct segments

            unicode.Segments.Clear();

            if (glyphIndexMap.Length > 0)
            {
                var startCode = glyphIndexMap[0].code;
                int startGlyphIndex = glyphIndexMap[0].glyphIndex;

                var endCode = startCode;

                for (var i = 1; i < glyphIndexMap.Length; i++)
                {
                    (var code, int glyphIndex) = glyphIndexMap[i];

                    if (code - startCode == glyphIndex - startGlyphIndex)
                    {
                        // in segment
                        endCode = code;
                    }
                    else
                    {
                        // add segment
                        var delta = startGlyphIndex - startCode;
                        var uDelta = delta >= 0 ? (ushort)delta : (ushort)(ushort.MaxValue + delta + 1);
                        unicode.Segments.Add(new TtfTableCmapSubtable4Segment
                        {
                            startCode = startCode,
                            endCode = endCode,
                            idDelta = uDelta,
                            idRangeOffset = 0
                        });

                        endCode = startCode = code;
                        startGlyphIndex = glyphIndex;
                    }
                }

                {
                    var delta = startGlyphIndex - startCode;
                    var uDelta = delta >= 0 ? (ushort)delta : (ushort)(ushort.MaxValue + delta + 1);
                    unicode.Segments.Add(new TtfTableCmapSubtable4Segment
                    {
                        startCode = startCode,
                        endCode = endCode,
                        idDelta = uDelta,
                        idRangeOffset = 0
                    });
                }
            }

            unicode.Segments.Add(new TtfTableCmapSubtable4Segment
            {
                startCode = 0xFFFF,
                endCode = 0xFFFF,
                idDelta = 0x0001,
                idRangeOffset = 0
            });

            // old macintosh compatibility?
            var zero = new TtfTableCmapSubtable0
            {
                Header = new TtfTableCmapSubtable0HeaderData
                {
                    language = 0
                }
            };

            for (var glyphIndex = 0; glyphIndex < sortedGlyphs.Length; glyphIndex++)
            {
                var entry = sortedGlyphs[glyphIndex];

                var code = entry.Key;

                if (code <= 255)
                    zero.GlyphIndexArray[code] = (byte)glyphIndex;
                else break;
            }


            var cmap = new TtfTableCmap();

            cmap.EncodingSubtables.Add(new TtfTableCmapEncodingSubtable(0, 3, unicode));
            cmap.EncodingSubtables.Add(new TtfTableCmapEncodingSubtable(1, 0, zero));
            cmap.EncodingSubtables.Add(new TtfTableCmapEncodingSubtable(3, 1, unicode));

            return cmap;
        }

        private (TtfTableGlyf, TtfTableHmtx) BuildGlyfAndHmtx()
        {
            var glyf = new TtfTableGlyf();
            var hmtx = new TtfTableHmtx();

            glyf.Entries.Capacity = NumGlyphs;
            hmtx.LongHorMetric.Capacity = NumGlyphs;

            void AddGlyph(IBuilderGlyph glyph)
            {
                glyf.Entries.Add(ToTrueTypeGlyph(glyph, out var width, out var lsb));
                hmtx.LongHorMetric.Add(new TtfTableHmtxLongHorMetricData
                {
                    advanceWidth = width,
                    leftSideBearing = lsb
                });
            }

            // unmapped
            AddGlyph(GlyphZero);
            AddGlyph(GlyphOne);

            // mapped
            foreach (var g in sortedGlyphs) AddGlyph(g.Value);

            return (glyf, hmtx);
        }

        private (TtfTableEblc, TtfTableEbdt) BuildEblcAndEbdt()
        {
            var eblc = new TtfTableEblc();
            var ebdt = new TtfTableEbdt();

            // max bitmap size is 127x127

            var size = new TtfTableEblcSize
            {
                Data = new TtfTableEblcBitmapSizeData
                {
                    colorRef = 0,
                    hori = new TtfTableEblcSbitLineMetricsData
                    {
                        ascender = (sbyte)((MetricHeight - Baseline) / PointScale),
                        descender = (sbyte)(-Baseline / PointScale),
                        widthMax = (byte)(MetricWidth / PointScale),
                        caretSlopeNumerator = 1,
                        caretSlopeDenominator = 0,
                        caretOffset = 0
                        /*
                        minOriginSB = 0,
                        minAdvanceSB = 0,
                        maxBeforeBL = (sbyte)(MetricHeight - Baseline),
                        minAfterBL = (sbyte)(-Baseline),
                        */
                    },
                    // NOTE: it is not supported
                    vert = new TtfTableEblcSbitLineMetricsData
                    {
                        ascender = (sbyte)(MetricWidth / 2 / PointScale),
                        descender = (sbyte)((MetricWidth / 2 - MetricWidth) / PointScale),
                        widthMax = (byte)(MetricWidth / PointScale),
                        caretSlopeNumerator = 1,
                        caretSlopeDenominator = 0,
                        caretOffset = 0
                        /*
                        minOriginSB = 0,
                        minAdvanceSB = 0,
                        maxBeforeBL = 0,
                        minAfterBL = 0,
                        */
                    },
                    /*
                    startGlyphIndex = ,
                    endGlyphIndex = ,
                    */
                    ppemX = (byte)(MetricHeight / PointScale),
                    ppemY = (byte)(MetricHeight / PointScale),
                    bitDepth = 1,
                    flags = TtfTableEblcBitmapFlags.HorizontalMetrics
                }
            };

            TtfTableEblcIndexSubtable1 currentSubtable = null;
            TtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmap2> currentSection = null;

            var prevIndex = -1;
            var glyphIndex = 0;

            void ProcessGlyphOrdered(IBuilderGlyph nextGlyph)
            {
                var currentIndex = glyphIndex++;
                if (TryGetTrueTypeBitmap(nextGlyph, out var bitmap))
                {
                    if (currentSection == null || prevIndex + 1 != currentIndex)
                    {
                        // new section
                        if (currentSection != null)
                        {
                            var lookup = currentSubtable.Lookup;
                            lookup.lastGlyphIndex = (ushort)prevIndex;
                            currentSubtable.Lookup = lookup;
                            currentSubtable.SectionTyped = currentSection;

                            size.Subtables.Add(currentSubtable);
                            ebdt.BitmapSections.Add(currentSection);
                        }

                        currentSection = new TtfTableEbdtGlyphBitmapSection<TtfTableEbdtGlyphBitmap2>();
                        currentSubtable = new TtfTableEblcIndexSubtable1(new TtfTableEblcIndexSubtableLookupData
                        {
                            firstGlyphIndex = (ushort)currentIndex
                        }, new TtfTableEblcIndexSubtableHeaderData());
                    }

                    currentSection.Bitmaps.Add(bitmap);

                    prevIndex = currentIndex;
                }
            }

            // unmapped
            ProcessGlyphOrdered(GlyphZero);
            ProcessGlyphOrdered(GlyphOne);

            // mapped
            foreach (var g in sortedGlyphs) ProcessGlyphOrdered(g.Value);

            // last section
            if (currentSection != null)
            {
                var lookup = currentSubtable.Lookup;
                lookup.lastGlyphIndex = (ushort)prevIndex;
                currentSubtable.Lookup = lookup;
                currentSubtable.SectionTyped = currentSection;

                size.Subtables.Add(currentSubtable);
                ebdt.BitmapSections.Add(currentSection);

                // add size
                eblc.Sizes.Add(size);
            }


            return (eblc, ebdt);
        }

        private TtfTableGasp BuildGasp()
        {
            var result = new TtfTableGasp();

            result.Ranges.Add(new TtfTableGaspRangeData()
            {
                rangeMaxPPEM = ushort.MaxValue,
                RangeGaspBehaviour = TtfTableGaspRangeBehaviour.Gridfit | TtfTableGaspRangeBehaviour.SymmetricGridfit
            });

            return result;
        }


        private void GetGlyphSize(IBuilderGlyph glyph, out ushort offset, out ushort width)
        {
            var contours = glyph.Contours;
            var isEmpty = !contours.Any();
            var widthMode = glyph.WidthMode;
            if (widthMode == IBuilderGlyph.WidthModeType.FullMonospace)
            {
                offset = 0;
                width = MetricWidth;
            }
            else if (widthMode == IBuilderGlyph.WidthModeType.TrimAuto)
            {
                if (isEmpty)
                {
                    offset = 0;
                    width = AutoTrimPadding > 0 ? AutoTrimPadding : MetricWidth;
                }
                else
                {
                    var xmin = ushort.MaxValue;
                    ushort xmax = 0;
                    foreach (var c in contours)
                    foreach (var p in c.Points)
                    {
                        if (p.x < xmin) xmin = p.x;
                        if (p.x > xmax) xmax = p.x;
                    }

                    offset = xmin;
                    width = (ushort)Math.Min(xmax - xmin + AutoTrimPadding, MetricWidth); //clamp
                }
            }
            else if (widthMode == IBuilderGlyph.WidthModeType.Manual)
            {
                offset = glyph.ManualOffset;
                width = glyph.ManualWidth;
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        private bool TryGetTrueTypeBitmap(IBuilderGlyph glyph, out TtfTableEbdtGlyphBitmap2 result)
        {
            GetGlyphSize(glyph, out var glyphOffset, out var glyphWidth);

            glyphOffset = (ushort)(glyphOffset / PointScale);
            glyphWidth = (ushort)(glyphWidth / PointScale);

            if (!glyph.TryGetBitmap(out var bitmap))
            {
                result = null;
                return false;
            }

            var sheet = bitmap.BitmapSheet;
            var sheetWidth = sheet.SheetWidth;
            var sheetHeight = sheet.SheetHeight;
            var sheetBitmap = sheet.Bitmap;
            if (sheetBitmap.Length != sheetWidth * sheetHeight)
                throw new ArgumentException(
                    $"Invalid bitmap size found {sheetBitmap.Length}, expected {sheetWidth * sheetHeight}");

            var bitmapX = bitmap.BitmapX;
            var bitmapY = bitmap.BitmapY;
            var width = bitmap.BitmapWidth;
            var height = bitmap.BitmapHeight;

            // trim
            var minX = width - 1;
            var maxX = 0;
            var minY = height - 1;
            var maxY = 0;
            var right = Math.Min(glyphOffset + glyphWidth, width);
            for (var y = 0; y < height; y++)
            {
                var yOnSheet = y + bitmapY;
                if (yOnSheet > sheetHeight) continue;

                // loop horizontal with determined size
                for (int x = glyphOffset; x < right; x++)
                {
                    var xOnSheet = x + bitmapX;
                    if (xOnSheet > sheetWidth) continue;

                    var b = sheetBitmap[yOnSheet * sheetWidth + xOnSheet];

                    if (b > 0)
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (maxX < x) maxX = x;
                        if (maxY < y) maxY = y;
                    }
                }
            }

            // empty
            if (minX < 0 || minY < 0 || maxX < minX || maxY < minY)
            {
                result = new TtfTableEbdtGlyphBitmap2();

                result.SmallMetrics = new TtfTableEbdtSmallGlyphMetricsData
                {
                    width = 1,
                    height = 1,
                    bearingX = 0,
                    bearingY = 1,
                    advance = (byte)glyphWidth
                };

                result.Bitmap = singleBitmap;

                return true;
            }

            var trimmedWidth = maxX - minX + 1;
            var trimmedHeight = maxY - minY + 1;

            var trimmedBitmap = new byte[trimmedWidth, trimmedHeight];
            for (var y = minY; y <= maxY; y++)
            {
                var yOnSheet = y + bitmapY;

                for (var x = minX; x <= maxX; x++)
                {
                    var xOnSheet = x + bitmapX;

                    trimmedBitmap[x - minX, maxY - y] = sheetBitmap[yOnSheet * sheetWidth + xOnSheet];
                }
            }


            result = new TtfTableEbdtGlyphBitmap2();

            result.SmallMetrics = new TtfTableEbdtSmallGlyphMetricsData
            {
                width = (byte)trimmedWidth,
                height = (byte)trimmedHeight,
                bearingX = (sbyte)(minX - glyphOffset),
                bearingY = (sbyte)(maxY + 1 - Baseline / PointScale),
                advance = (byte)glyphWidth
            };

            result.Bitmap = trimmedBitmap;

            return true;
        }

        private TtfTableGlyfEntrySimple ToTrueTypeGlyph(IBuilderGlyph builderGlyph, out ushort width, out short lsb)
        {
            var contours = builderGlyph.Contours;
            var isEmpty = !contours.Any();

            GetGlyphSize(builderGlyph, out var offset, out width);

            if (isEmpty)
            {
                lsb = 0;
                return null;
            }

            var result = new TtfTableGlyfEntrySimple(new TtfTableGlyfHeaderData());

            var points = result.Points;
            var endPtsOfContours = result.EndPtsOfContours;

            var pointIndex = 0;
            short cursorX = 0;
            short cursorY = 0;
            var xMin = short.MaxValue;
            var yMin = short.MaxValue;
            short xMax = 0;
            short yMax = 0;
            foreach (var c in contours)
            {
                foreach (var p in c.Points)
                {
                    var px = (short)(p.x - offset);
                    var py = (short)(p.y - Baseline);
                    var dx = (short)(px - cursorX);
                    var dy = (short)(py - cursorY);
                    cursorX = px;
                    cursorY = py;

                    if (px < xMin) xMin = px;
                    if (py < yMin) yMin = py;
                    if (px > xMax) xMax = px;
                    if (py > yMax) yMax = py;

                    points.Add(new TtfTableGlyfSimpleGlyphPoint
                    {
                        onCurve = true,
                        x = new TtfTableGlyfSimpleGlyphPoint.Element
                        {
                            CoordValue = dx
                        },
                        y = new TtfTableGlyfSimpleGlyphPoint.Element
                        {
                            CoordValue = dy
                        }
                    });
                    pointIndex++;
                }

                endPtsOfContours.Add((ushort)(pointIndex - 1));
            }

            result.Header = new TtfTableGlyfHeaderData
            {
                xMin = xMin,
                yMin = yMin,
                xMax = xMax,
                yMax = yMax
            };

            lsb = xMin;

            return result;
        }
    }
}