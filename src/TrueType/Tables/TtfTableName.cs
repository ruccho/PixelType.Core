using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PixelType.TrueType
{
    public class TtfTableName : TrueTypeTable
    {
        public override uint Tag => TrueTypeFont.ToTableTag("name");

        public TtfTableNameHeaderData Header { get; set; }

        public List<TtfTableNameRecord> Records { get; } = new();

        public override void Validate(ValidationContext context)
        {
            Records.RemoveAll(r => r.NameBytes.Length == 0);
        }

        public override long GetSize()
        {
            var headerSize = Marshal.SizeOf<TtfTableNameHeaderData>();
            var recordCount = Records.Count;
            var recordSize = Marshal.SizeOf<TtfTableNameRecordData>();
            var recordsSize = recordCount * recordSize;

            var sumStrings = 0;
            foreach (var r in Records) sumStrings += r.NameBytes.Length;

            return headerSize + recordsSize + sumStrings;
        }

        public override void Serialize(Span<byte> dest)
        {
            var header = Header;
            header.format = 0;

            int recordCount = header.count = (ushort)Records.Count;
            var headerSize = Marshal.SizeOf<TtfTableNameHeaderData>();
            var recordSize = Marshal.SizeOf<TtfTableNameRecordData>();
            var recordsSize = recordCount * recordSize;

            header.stringOffset = (ushort)(headerSize + recordsSize);
            Header = header;

            Utils.Serialize(Header, dest, 0, out _);


            var recordsSlice = MemoryMarshal.Cast<byte, TtfTableNameRecordData>(dest.Slice(headerSize, recordsSize));
            var stringsSlice = dest.Slice(header.stringOffset);

            var stringCursor = 0;
            for (var i = 0; i < recordCount; i++)
            {
                var record = Records[i];
                var nameBytes = record.NameBytes;
                var stringSize = nameBytes.Length;

                var stringSlice = stringsSlice.Slice(stringCursor, stringSize);
                nameBytes.CopyTo(stringSlice);

                recordsSlice[i] = new TtfTableNameRecordData
                {
                    platformId = record.PlatformId,
                    platformSpecificId = record.PlatformSpecificId,
                    languageId = record.LanguageId,
                    nameId = record.NameId,
                    length = (ushort)nameBytes.Length,
                    offset = (ushort)stringCursor
                };

                stringCursor += stringSize;
            }
        }

        public override void Deserialize(DeserializationContext context, ref BufferReader data)
        {
            data.ReadUnaligned(out TtfTableNameHeaderData header);
            Header = header;

            int recordCount = Header.count;

            var stringsSlice = data.SliceFromStart(Header.stringOffset).ReadBytesAsPossible();

            Records.Clear();
            for (var i = 0; i < recordCount; i++)
            {
                data.ReadUnaligned(out TtfTableNameRecordData record);
                Records.Add(new TtfTableNameRecord(record, stringsSlice));
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableNameHeaderData
    {
        public U16 format;
        public U16 count;
        public U16 stringOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableNameRecordData
    {
        public U16 platformId;
        public U16 platformSpecificId;
        public U16 languageId;
        public U16 nameId;
        public U16 length;
        public U16 offset;
    }

    public class TtfTableNameRecord
    {
        public TtfTableNameRecord()
        {
            NameBytes = Array.Empty<byte>();
        }

        public TtfTableNameRecord(ushort platformId, ushort platformSpecificId, ushort languageId, NameIdType nameId,
            string name, Encoding encoding) : this(platformId, platformSpecificId, languageId, (ushort)nameId, name,
            encoding)
        {
        }

        public TtfTableNameRecord(ushort platformId, ushort platformSpecificId, ushort languageId, ushort nameId,
            string name, Encoding encoding)
        {
            PlatformId = platformId;
            PlatformSpecificId = platformSpecificId;
            LanguageId = languageId;
            NameId = nameId;
            NameBytes = encoding.GetBytes(name);
        }

        public TtfTableNameRecord(TtfTableNameRecordData record, ReadOnlySpan<byte> stringsSlice)
        {
            PlatformId = record.platformId;
            PlatformSpecificId = record.platformSpecificId;
            LanguageId = record.languageId;
            NameId = record.nameId;

            NameBytes = stringsSlice.Slice(record.offset, record.length).ToArray();
        }

        public ushort PlatformId { get; set; }
        public ushort PlatformSpecificId { get; set; }
        public ushort LanguageId { get; set; }
        public ushort NameId { get; set; }

        public byte[] NameBytes { get; set; }
    }

    public enum NameIdType
    {
        CopyrightNotice = 0,
        FontFamily,
        FontSubfamily,
        SubfamilyId,
        FullName,
        VersionOfNameTable,
        PostScriptName,
        TrademarkNotice,
        Manufacturer,
        Designer,
        Description,
        VendorUrl,
        DesignerUrl,
        LicenseDescription,
        LicenseUrl,
        PreferredFamily = 16,
        PreferredSubfamily,
        CompatibleFull,
        SampleText,
        PostScriptCidFindfontName = 20,
        WwsFamilyName,
        WwsSubfamilyName,
        LightBackgroundPalette,
        DarkBackgroundPalette,
        VariationsPostScriptNamePrefix = 25
    }
}