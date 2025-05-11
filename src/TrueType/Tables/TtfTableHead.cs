using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PixelType.TrueType
{
    public class TtfTableHead : TrueTypeTableFixed<TtfTableHeadData>
    {
        public override uint Tag => TrueTypeFont.ToTableTag("head");

        public override Type[] ValidationDependencies { get; } = { typeof(TtfTableLoca) };

        public override void Validate(ValidationContext context)
        {
            var loca = context.ValidatedTables.OfType<TtfTableLoca>().First();
            var data = Data;
            data.indexToLocFormat = loca.FormatMode switch
            {
                TtfTableLoca.FormatModeType.Short => 0,
                TtfTableLoca.FormatModeType.Long => 1,
                _ => throw new ArgumentOutOfRangeException()
            };

            Data = data;
        }

        public override void Serialize(Span<byte> dest)
        {
            var data = Data;

            data.version = Fixed.FromInt32(0x00010000);
            data.magicNumber = 0x5F0F3CF5;
            data.glyphDataFormat = 0;

            Data = data;
            base.Serialize(dest);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TtfTableHeadData
    {
        public Fixed version;
        public Fixed fontRevision;
        public U32 checkSumAdjustment;
        public U32 magicNumber;
        private U16 flags;
        public U16 unitsPerEm;
        public LongDateTime created;
        public LongDateTime modified;

        public FWord xMin;
        public FWord yMin;
        public FWord xMax;
        public FWord yMax;

        public U16 macStyle;
        public U16 lowestRecPPEM;
        public I16 fontDirectionHint;
        public I16 indexToLocFormat;
        public I16 glyphDataFormat;

        public bool BaselineIsZero
        {
            get => GetFlag(0);
            set => SetFlag(0, value);
        }

        public bool LeftSidebearingIsZero
        {
            get => GetFlag(1);
            set => SetFlag(1, value);
        }

        public bool ScaledPointSizeDiffer
        {
            get => GetFlag(2);
            set => SetFlag(2, value);
        }

        public bool UseIntegerScaling
        {
            get => GetFlag(3);
            set => SetFlag(3, value);
        }

        public bool InstructionAlterAdvanceWidth
        {
            get => GetFlag(4);
            set => SetFlag(4, value);
        }

        public bool IsVertical
        {
            get => GetFlag(5);
            set => SetFlag(5, value);
        }

        public bool RequireLinguisticLayout
        {
            get => GetFlag(7);
            set => SetFlag(7, value);
        }

        public bool UseAATMetamorphicEffectByDefault
        {
            get => GetFlag(8);
            set => SetFlag(8, value);
        }

        public bool ContainsStrongRTLGlyph
        {
            get => GetFlag(9);
            set => SetFlag(9, value);
        }

        public bool ContainsIndicStyleRearrangement
        {
            get => GetFlag(10);
            set => SetFlag(10, value);
        }

        public bool OptimizedForClearType
        {
            get => GetFlag(13);
            set => SetFlag(13, value);
        }

        public bool IsGenericSymbol
        {
            get => GetFlag(14);
            set => SetFlag(14, value);
        }

        public bool StyleBold
        {
            get => GetStyleFlag(0);
            set => SetStyleFlag(0, value);
        }

        public bool StyleItalic
        {
            get => GetStyleFlag(1);
            set => SetStyleFlag(1, value);
        }

        public bool StyleUnderline
        {
            get => GetStyleFlag(2);
            set => SetStyleFlag(2, value);
        }

        public bool StyleOutline
        {
            get => GetStyleFlag(3);
            set => SetStyleFlag(3, value);
        }

        public bool StyleShadow
        {
            get => GetStyleFlag(4);
            set => SetStyleFlag(4, value);
        }

        public bool StyleCondensed
        {
            get => GetStyleFlag(5);
            set => SetStyleFlag(5, value);
        }

        public bool StyleExtended
        {
            get => GetStyleFlag(6);
            set => SetStyleFlag(6, value);
        }

        public DirectionHintType DirectionHint
        {
            get => (DirectionHintType)(short)fontDirectionHint;
            set => fontDirectionHint = (short)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetFlag(int digit)
        {
            return (flags & (1 << digit)) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFlag(int digit, bool value)
        {
            flags = (ushort)(value ? flags | (1 << digit) : flags & ~(1 << digit));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetStyleFlag(int digit)
        {
            return (macStyle & (1 << digit)) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetStyleFlag(int digit, bool value)
        {
            macStyle = (ushort)(value ? macStyle | (1 << digit) : macStyle & ~(1 << digit));
        }

        public enum DirectionHintType
        {
            StronglyRTLAndNeutrals = -2,
            OnlyStronglyRTL = -1,
            Mixed = 0,
            OnlyStronglyLTR = 1,
            StronglyLTRAndNeutrals = 2
        }
    }
}