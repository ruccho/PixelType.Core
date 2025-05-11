using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelType
{
    public class EmptyGlyph : IBuilderGlyph, IBuilderGlyphBitmap, IBuilderBitmapSheet
    {
        public EmptyGlyph()
        {
            WidthMode = IBuilderGlyph.WidthModeType.FullMonospace;
        }

        public EmptyGlyph(ushort width)
        {
            WidthMode = IBuilderGlyph.WidthModeType.Manual;
            ManualWidth = width;
        }

        public int SheetWidth => 0;
        public int SheetHeight => 0;
        public ReadOnlySpan<byte> Bitmap => Array.Empty<byte>();

        public IBuilderGlyph.WidthModeType WidthMode { get; }
        public ushort ManualOffset { get; }
        public ushort ManualWidth { get; }
        public IEnumerable<IBuilderGlyphContour> Contours => Enumerable.Empty<IBuilderGlyphContour>();

        public bool TryGetBitmap(out IBuilderGlyphBitmap bitmap)
        {
            bitmap = this;
            return true;
        }

        public int BitmapWidth => 0;
        public int BitmapHeight => 0;
        public int BitmapX => 0;
        public int BitmapY => 0;

        public IBuilderBitmapSheet BitmapSheet => this;
    }
}