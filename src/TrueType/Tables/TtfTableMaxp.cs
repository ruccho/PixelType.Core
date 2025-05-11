using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace PixelType.TrueType
{
    public class TtfTableMaxp : TrueTypeTableFixed<TtfTableMaxpData>
    {
        public override uint Tag => TrueTypeFont.ToTableTag("maxp");
        public override Type[] ValidationDependencies { get; } = { typeof(TtfTableGlyf) };

        public override void Validate(ValidationContext context)
        {
            var data = Data;
            var glyf = context.ValidatedTables.OfType<TtfTableGlyf>().First();
            data.numGlyphs = (ushort)glyf.Entries.Count;

            ushort maxPoints = 0;
            ushort maxContours = 0;
            foreach (var g in glyf.Entries.OfType<TtfTableGlyfEntrySimple>())
            {
                var numPoints = (ushort)g.Points.Count;
                var numContours = (ushort)g.EndPtsOfContours.Count;
                if (maxPoints < numPoints) maxPoints = numPoints;
                if (maxContours < numContours) maxContours = numContours;
            }

            //TODO: support compound glyph
            ushort maxComponentPoints = 0;
            ushort maxComponentContours = 0;

            data.maxPoints = maxPoints;
            data.maxContours = maxContours;
            data.maxComponentPoints = maxComponentPoints;
            data.maxComponentContours = maxComponentContours;

            Data = data;
        }

        public override void Serialize(Span<byte> dest)
        {
            var data = Data;

            data.version = Fixed.FromInt32(0x00010000);

            Data = data;

            base.Serialize(dest);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableMaxpData
    {
        public Fixed version;
        public U16 numGlyphs;
        public U16 maxPoints;
        public U16 maxContours;
        public U16 maxComponentPoints;
        public U16 maxComponentContours;
        public U16 maxZones;
        public U16 maxTwilightPoints;
        public U16 maxStorage;
        public U16 maxFunctionDefs;
        public U16 maxInstructionDefs;
        public U16 maxStackElements;
        public U16 maxSizeOfInstructions;
        public U16 maxComponentElements;
        public U16 maxComponentDepth;
    }
}