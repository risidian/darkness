namespace Darkness.Core.Interfaces
{
    public interface ISpriteCompositor
    {
        byte[] CompositeLayers(IReadOnlyList<Stream> layerStreams, int sheetWidth, int sheetHeight);
        byte[] ExtractFrame(byte[] spriteSheetPng, int frameX, int frameY, int frameWidth, int frameHeight, int scale);
    }
}
