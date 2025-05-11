using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PixelType.TrueType
{
    public class TrueTypeFont
    {
        private static readonly Dictionary<uint, Func<TrueTypeTable>> TableCreators =
            new()
            {
                // required
                { ToTableTag("head"), () => new TtfTableHead() },
                { ToTableTag("cmap"), () => new TtfTableCmap() },
                { ToTableTag("hhea"), () => new TtfTableHhea() },
                { ToTableTag("hmtx"), () => new TtfTableHmtx() },
                { ToTableTag("maxp"), () => new TtfTableMaxp() },
                { ToTableTag("loca"), () => new TtfTableLoca() },
                { ToTableTag("glyf"), () => new TtfTableGlyf() },
                { ToTableTag("name"), () => new TtfTableName() },
                { ToTableTag("post"), () => new TtfTablePost() },
                { ToTableTag("OS/2"), () => new TtfTableOs2() },

                // optional
                { ToTableTag("gasp"), () => new TtfTableGasp() },
                { ToTableTag("EBLC"), () => new TtfTableEblc() },
                { ToTableTag("EBDT"), () => new TtfTableEbdt() }
            };

        public List<TrueTypeTable> Tables { get; set; }

        public static void RegisterTableDefinition(string tag, Func<TrueTypeTable> creator)
        {
            TableCreators[ToTableTag(tag)] = creator;
        }

        internal static uint ToTableTag(string tag)
        {
            Span<byte> tagBytes = stackalloc byte[4];
            Encoding.ASCII.GetBytes(tag, tagBytes);
            var tagBytesU32 = MemoryMarshal.Cast<byte, U32>(tagBytes);
            return tagBytesU32[0];
        }

        private static TrueTypeTable CreateTableInstance(TableDirectoryEntry entry)
        {
            return TableCreators.TryGetValue(entry.tag, out var creator)
                ? creator()
                : new TrueTypeTableUnknown(entry.tag);
        }

        private static void ValidateChecksum(ReadOnlySpan<byte> font, TableDirectoryEntry entry)
        {
            var offset = (int)(uint)entry.offset;
            var length = (int)(uint)entry.length;

            var isHead = entry.tag == ToTableTag("head");

            var checkSumSize = (length + 3) / 4 * 4;
            var checkSumSpan = font.Slice(offset, checkSumSize);
            var checkSumSpanBe = MemoryMarshal.Cast<byte, U32>(checkSumSpan);
            var checkSumAdjustment = isHead ? (uint)checkSumSpanBe[2] : 0;
            var checkSum = isHead ? uint.MaxValue - checkSumAdjustment + 1 : 0;
            unchecked
            {
                foreach (var c in checkSumSpanBe) checkSum += c;
            }

            if (checkSum != entry.checkSum)
                throw new ArgumentException(
                    $"Checksum of table 0x{offset:X} mismatch where an expected is {checkSum:X}, an actual is {(uint)entry.checkSum:X}.");
        }

        private static void DeserializeTable(DeserializationContext context, ReadOnlySpan<byte> font,
            TableDirectoryEntry entry,
            TrueTypeTable table)
        {
            var offset = (int)(uint)entry.offset;
            var length = (int)(uint)entry.length;

            var dataSpan = font.Slice(offset, length);
            var reader = new BufferReader(dataSpan);

            table.Deserialize(context, ref reader);
            context.DeserializedTables.Add(table);
        }

        public static TrueTypeFont Deserialize(ReadOnlySpan<byte> font)
        {
            var context = new DeserializationContext();

            var sizeOfOffsetTable = Marshal.SizeOf<OffsetSubtable>();
            var offsetTableSpan = font.Slice(0, sizeOfOffsetTable);

            var offsetTable = MemoryMarshal.Read<OffsetSubtable>(offsetTableSpan);

            var sizeOfTableEntry = Marshal.SizeOf<TableDirectoryEntry>();
            var tableDirectoryOffset = sizeOfOffsetTable;

            var tables = new (TableDirectoryEntry entry, TrueTypeTable table)[offsetTable.numTables];

            for (var i = 0; i < offsetTable.numTables; i++)
            {
                var entryOffset = tableDirectoryOffset + i * sizeOfTableEntry;
                var entrySpan = font.Slice(entryOffset, sizeOfTableEntry);
                var entry = MemoryMarshal.Read<TableDirectoryEntry>(entrySpan);

                ValidateChecksum(font, entry);
                var table = CreateTableInstance(entry);

                tables[i] = (entry, table);
            }

            var toBeProcessed = new bool[offsetTable.numTables];
            var processed = new bool[offsetTable.numTables];

            for (var i = 0; i < offsetTable.numTables; i++)
            {
                void DeserializeTableRecursive(int index, ReadOnlySpan<byte> fontData)
                {
                    if (processed[index]) return; // already processed
                    if (toBeProcessed[index])
                        throw new InvalidOperationException("Cyclic dependency cannot be resolved!");
                    toBeProcessed[index] = true;

                    var set = tables[index];

                    foreach (var depType in set.table.DeserializationDependencies)
                    {
                        int depIndex;
                        for (depIndex = 0; depIndex < tables.Length; depIndex++)
                            if (tables[depIndex].table.GetType() == depType)
                                break;
                        if (depIndex < tables.Length)
                            DeserializeTableRecursive(depIndex, fontData);
                        else
                            throw new InvalidOperationException(
                                $"Dependency missing. Table \"{depType.Name}\" is required by \"{set.table.GetType().Name}\"");
                    }

                    DeserializeTable(context, fontData, set.entry, set.table);
                    processed[index] = true;
                }

                DeserializeTableRecursive(i, font);
            }

            // context.DeserializedTables.RemoveAll(t => t is TrueTypeTableUnknown);

            return new TrueTypeFont
            {
                Tables = context.DeserializedTables
            };
        }

        public void Validate()
        {
            Tables.Sort((a, b) =>
            {
                if (b.Tag < a.Tag) return 1;
                if (b.Tag > a.Tag) return -1;
                return 0;
            });

            var tables = Tables;
            var numTables = tables.Count;
            var toBeProcessed = new bool[numTables];
            var processed = new bool[numTables];

            var context = new ValidationContext();

            void ValidateTableRecursive(int index)
            {
                if (processed[index]) return; // already processed
                if (toBeProcessed[index]) throw new InvalidOperationException("Cyclic dependency cannot be resolved!");
                toBeProcessed[index] = true;

                var table = tables[index];

                foreach (var depType in table.ValidationDependencies)
                {
                    int depIndex;
                    for (depIndex = 0; depIndex < numTables; depIndex++)
                        if (tables[depIndex].GetType() == depType)
                            break;
                    if (depIndex < numTables)
                        ValidateTableRecursive(depIndex);
                    else
                        throw new InvalidOperationException(
                            $"Dependency missing. Table \"{depType.Name}\" is required by \"{table.GetType().Name}\"");
                }

                table.Validate(context);
                context.ValidatedTables.Add(table);
                processed[index] = true;
            }

            for (var i = 0; i < numTables; i++) ValidateTableRecursive(i);
        }

        public long GetSize(bool skipValidation = false)
        {
            if (!skipValidation) Validate();

            var sizeOfOffsetTable = Marshal.SizeOf<OffsetSubtable>();
            var sizeOfTableEntry = Marshal.SizeOf<TableDirectoryEntry>();

            var numTables = Tables.Count;

            long sumTableSize = 0;
            foreach (var table in Tables) sumTableSize += (table.GetSize() + 3) / 4 * 4;

            return sizeOfOffsetTable +
                   sizeOfTableEntry * numTables +
                   sumTableSize;
        }

        public void Serialize(Span<byte> dest, bool skipValidation = false)
        {
            if (!skipValidation) Validate();

            var sizeOfOffsetTable = Marshal.SizeOf<OffsetSubtable>();
            var offsetTableSpan = dest.Slice(0, sizeOfOffsetTable);

            var numTables = Tables.Count;

            var offsetTable = new OffsetSubtable((ushort)numTables);

            MemoryMarshal.Write(offsetTableSpan, ref offsetTable);

            var sizeOfTableEntry = Marshal.SizeOf<TableDirectoryEntry>();
            var tableDirectoryOffset = sizeOfOffsetTable;
            var tableDataOffset = sizeOfOffsetTable + sizeOfTableEntry * numTables;
            var tableDataOffsetCursor = tableDataOffset;

            var checkSumAdjustment = Span<U32>.Empty;
            for (var i = 0; i < numTables; i++)
            {
                var offset = tableDirectoryOffset + i * sizeOfTableEntry;
                var entrySpan = dest.Slice(offset, sizeOfTableEntry);
                var table = Tables[i];
                var length = (uint)table.GetSize();

                var dataSpan = dest.Slice(tableDataOffsetCursor, (int)length);
                table.Serialize(dataSpan);

                var isHead = table.Tag == ToTableTag("head");

                if (isHead)
                {
                    checkSumAdjustment = MemoryMarshal.Cast<byte, U32>(dataSpan.Slice(8, 4));
                    checkSumAdjustment[0] = 0;
                }

                var checkSumSize = (length + 3) / 4 * 4;
                var checkSumSpan = dest.Slice(tableDataOffsetCursor, (int)checkSumSize);
                var checkSumSpanBe = MemoryMarshal.Cast<byte, U32>(checkSumSpan);
                uint checkSum = 0;
                unchecked
                {
                    foreach (var c in checkSumSpanBe) checkSum += c;
                }

                var entry = new TableDirectoryEntry
                {
                    tag = table.Tag,
                    offset = (uint)tableDataOffsetCursor,
                    length = length,
                    checkSum = checkSum
                };

                MemoryMarshal.Write(entrySpan, ref entry);

                tableDataOffsetCursor += (int)length;

                // alignment
                tableDataOffsetCursor = (tableDataOffsetCursor + 3) / 4 * 4;
            }

            if (!checkSumAdjustment.IsEmpty)
            {
                var destU32 = MemoryMarshal.Cast<byte, U32>(dest);
                uint checkSum = 0;
                unchecked
                {
                    foreach (var t in destU32) checkSum += t;

                    checkSum = 0xB1B0AFBA - checkSum;
                }

                checkSumAdjustment[0] = checkSum;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct OffsetSubtable
        {
            public readonly U32 scalerType;
            public readonly U16 numTables;
            public readonly U16 searchRange;
            public readonly U16 entrySelector;
            public readonly U16 rangeShift;

            public OffsetSubtable(ushort numTables)
            {
                scalerType = 0x00010000;
                this.numTables = numTables;

                if (numTables == 0) throw new ArgumentException();

                var p = numTables;
                int i;
                for (i = 0; i < 16; i++)
                {
                    if (p == 0) break;
                    p >>= 1;
                }

                var msb = i - 1;
                var mp = 1 << msb;

                var searchRange = (ushort)(mp * 16);
                this.searchRange = searchRange;
                entrySelector = (ushort)msb;
                rangeShift = (ushort)(numTables * 16 - searchRange);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TableDirectoryEntry
        {
            public U32 tag;
            public U32 checkSum;
            public U32 offset;
            public U32 length;
        }
    }
}