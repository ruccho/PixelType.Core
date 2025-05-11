using System;

namespace PixelType
{
    public interface IBuilderBitmapSheet
    {
        int SheetWidth { get; }
        int SheetHeight { get; }
        ReadOnlySpan<byte> Bitmap { get; }
    }
}