using Darkness.Core.Models;

namespace Darkness.Core.Interfaces
{
    public interface ISpriteLayerCatalog
    {
        List<SpriteLayerDefinition> GetLayersForAppearance(CharacterAppearance appearance);
        CharacterAppearance GetDefaultAppearanceForClass(string className);
        List<string> HairStyles { get; }
        List<string> HairColors { get; }
        List<string> SkinColors { get; }
        List<string> ArmorTypes { get; }
        List<string> WeaponTypes { get; }
    }
}
