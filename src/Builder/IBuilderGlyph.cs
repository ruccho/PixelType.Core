using System.Collections.Generic;

namespace PixelType
{
    public interface IBuilderGlyph
    {
        public enum WidthModeType
        {
            FullMonospace,
            TrimAuto,
            Manual
        }

        WidthModeType WidthMode { get; }
        ushort ManualOffset { get; }
        ushort ManualWidth { get; }
        IEnumerable<IBuilderGlyphContour> Contours { get; }

        bool TryGetBitmap(out IBuilderGlyphBitmap bitmap);
    }
}