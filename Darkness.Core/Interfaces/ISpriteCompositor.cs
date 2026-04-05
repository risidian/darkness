using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces
{
    public interface ISpriteCompositor
    {
        byte[] CompositeLayers(IReadOnlyList<Stream> layerStreams, int sheetWidth, int sheetHeight);
        Task<byte[]> CompositeFullSheet(IReadOnlyList<StitchLayer> layers, IFileSystemService fileSystem);
        Task<byte[]> CompositePreviewFrame(IReadOnlyList<StitchLayer> layers, IFileSystemService fileSystem);
        byte[] ExtractFrame(byte[] spriteSheetPng, int frameX, int frameY, int frameWidth, int frameHeight, int scale);
    }
}