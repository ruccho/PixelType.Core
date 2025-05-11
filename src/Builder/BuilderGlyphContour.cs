using System.Collections.Generic;

namespace PixelType
{
    public class BuilderGlyphContour : IBuilderGlyphContour
    {
        public BuilderGlyphContour()
        {
        }

        public BuilderGlyphContour(List<(ushort x, ushort y)> points)
        {
            PointList = points;
        }

        public List<(ushort x, ushort y)> PointList { get; } = new();

        public IEnumerable<(ushort x, ushort y)> Points => PointList;
    }
}