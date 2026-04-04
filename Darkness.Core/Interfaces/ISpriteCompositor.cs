namespace Darkness.Core.Interfaces
{
    public interface ISpriteCompositor
    {
        byte[] CompositeLayers(IReadOnlyList<Stream> layerStreams, int sheetWidth, int sheetHeight);
        Task<byte[]> CompositeFullSheet(IReadOnlyList<string> layerBasePaths, IFileSystemService fileSystem);
        byte[] ExtractFrame(byte[] spriteSheetPng, int frameX, int frameY, int frameWidth, int frameHeight, int scale);
    }
}
