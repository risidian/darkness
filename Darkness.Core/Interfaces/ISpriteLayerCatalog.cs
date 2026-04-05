using Darkness.Core.Models;

namespace Darkness.Core.Interfaces
{
    public interface ISpriteLayerCatalog
    {
        List<SpriteLayerDefinition> GetLayersForAppearance(CharacterAppearance appearance);
        List<StitchLayer> GetStitchLayers(CharacterAppearance appearance);
        CharacterAppearance GetDefaultAppearanceForClass(string className);
        List<string> HairStyles { get; }
        List<string> HairColors { get; }
        List<string> SkinColors { get; }
        List<string> FaceTypes { get; }
        List<string> EyeTypes { get; }
        List<string> HeadTypes { get; }
        List<string> FeetTypes { get; }
        List<string> ArmsTypes { get; }
        List<string> LegsTypes { get; }
        List<string> ArmorTypes { get; }
        List<string> WeaponTypes { get; }
        List<string> ShieldTypes { get; }
    }
}