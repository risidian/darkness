using System.Collections.Generic;
using System.Threading.Tasks;
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces
{
    public interface ISpriteCompositor
    {
        Task<byte[]> CompositeFullSheet(
            IReadOnlyList<SheetDefinition> definitions,
            CharacterAppearance appearance,
            IFileSystemService fileSystem);

        Task<byte[]> CompositePreviewFrame(
            IReadOnlyList<SheetDefinition> definitions,
            CharacterAppearance appearance,
            IFileSystemService fileSystem);

        byte[] ExtractFrame(byte[] spriteSheetPng, string animation, int frameIndex, int direction);
    }
}
