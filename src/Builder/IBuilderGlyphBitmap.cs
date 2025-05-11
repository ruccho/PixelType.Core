namespace PixelType
{
    public interface IBuilderGlyphBitmap
    {
        int BitmapWidth { get; }
        int BitmapHeight { get; }
        int BitmapX { get; }
        int BitmapY { get; }
        IBuilderBitmapSheet BitmapSheet { get; }
    }
}