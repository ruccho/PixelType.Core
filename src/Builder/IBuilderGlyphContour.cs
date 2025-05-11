using System.Collections.Generic;

namespace PixelType
{
    public interface IBuilderGlyphContour
    {
        IEnumerable<(ushort x, ushort y)> Points { get; }
    }
}