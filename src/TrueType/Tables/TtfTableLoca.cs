using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PixelType.TrueType
{
    public class TtfTableLoca : TrueTypeTable
    {
        public enum FormatModeType
        {
            Short,
            Long
        }

        public override Type[] DeserializationDependencies { get; } =
        {
            typeof(TtfTableHead),
            typeof(TtfTableMaxp)
        };

        public override Type[] ValidationDependencies { get; } =
        {
            typeof(TtfTableGlyf)
        };

        public override uint Tag => TrueTypeFont.ToTableTag("loca");

        public List<uint> Offsets { get; } = new();
        public FormatModeType FormatMode { get; set; }

        public override void Validate(ValidationContext context)
        {
            var glyf = context.ValidatedTables.OfType<TtfTableGlyf>().First();
            long cursor = 0;
            var useLongFormat = false;
            var align = glyf.Align;

            foreach (var e in glyf.Entries)
            {
                if (e == null) continue;
                var size = e.GetSize();
                if (align) size = (size + 3) / 4 * 4;
                cursor += size;
                if ((cursor & 0x1) > 0 || cursor > ushort.MaxValue)
                {
                    useLongFormat = true;
                    break;
                }
            }

            FormatMode = useLongFormat ? FormatModeType.Long : FormatModeType.Short;
            Offsets.Clear();
            Offsets.Capacity = glyf.Entries.Count + 1;

            cursor = 0;
            Offsets.Add(0);
            foreach (var e in glyf.Entries)
            {
                if (e != null)
                {
                    var size = e.GetSize();
                    if (align) size = (size + 3) / 4 * 4;
                    cursor += size;
                }

                Offsets.Add(useLongFormat ? (uint)cursor : (ushort)(cursor >> 1));
            }
        }

        public override long GetSize()
        {
            if (FormatMode == FormatModeType.Long)
                return Offsets.Count * sizeof(uint);
            if (FormatMode == FormatModeType.Short)
                return Offsets.Count * sizeof(ushort);
            throw new ArgumentOutOfRangeException();
        }

        public override void Serialize(Span<byte> dest)
        {
            if (dest.Length != GetSize()) throw new ArgumentException();

            if (FormatMode == FormatModeType.Long)
            {
                var slice = MemoryMarshal.Cast<byte, U32>(dest);
                for (var i = 0; i < Offsets.Count; i++) slice[i] = Offsets[i];
            }
            else if (FormatMode == FormatModeType.Short)
            {
                var slice = MemoryMarshal.Cast<byte, U16>(dest);
                for (var i = 0; i < Offsets.Count; i++) slice[i] = (ushort)Offsets[i];
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public override void Deserialize(DeserializationContext context, ref BufferReader data)
        {
            var head = context.DeserializedTables.OfType<TtfTableHead>().First();
            var maxp = context.DeserializedTables.OfType<TtfTableMaxp>().First();

            var n = maxp.Data.numGlyphs + 1;

            var isLongFormat = head.Data.indexToLocFormat > 0;
            var elementSize = isLongFormat ? sizeof(uint) : sizeof(ushort);
            var size = elementSize * n;

            if (data.Remains != size) throw new ArgumentException();


            Offsets.Clear();
            if (isLongFormat)
            {
                var slice = data.ReadUnalignedAsPossible<U32>();
                // var slice = MemoryMarshal.Cast<byte, U32>(data);
                Offsets.Capacity = slice.Length;
                foreach (var t in slice) Offsets.Add(t);

                FormatMode = FormatModeType.Long;
            }
            else
            {
                var slice = data.ReadUnalignedAsPossible<U16>();
                // var slice = MemoryMarshal.Cast<byte, U16>(data);
                Offsets.Capacity = slice.Length;
                foreach (var t in slice) Offsets.Add(t);

                FormatMode = FormatModeType.Short;
            }
        }
    }
}