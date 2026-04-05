using Darkness.Core.Models;

namespace Darkness.Core.Interfaces
{
    public interface ISpriteLayerCatalog
    {
        List<StitchLayer> GetStitchLayers(CharacterAppearance appearance);
        CharacterAppearance GetDefaultAppearanceForClass(string className);
        List<string> GetOptionNames(string category);
    }
}
