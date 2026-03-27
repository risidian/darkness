using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services
{
    public class SpriteLayerCatalog : ISpriteLayerCatalog
    {
        private const int ZBody = 10;
        private const int ZArmor = 40;
        private const int ZHair = 80;
        private const int ZWeapon = 100;

        public List<string> HairStyles { get; } = new() { "Long", "Short", "Mohawk", "Messy" };

        public List<SpriteLayerDefinition> GetLayersForAppearance(CharacterAppearance appearance)
        {
            var layers = new List<SpriteLayerDefinition>
            {
                new($"sprites/body/{appearance.SkinColor.ToLower()}.png", ZBody),
                new($"sprites/hair/{appearance.HairStyle.ToLower()}_{appearance.HairColor.ToLower()}.png", ZHair),
                new($"sprites/armor/{appearance.ArmorType.ToLower()}.png", ZArmor),
                new($"sprites/weapons/{appearance.WeaponType.ToLower()}.png", ZWeapon),
            };

            layers.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
            return layers;
        }

        public CharacterAppearance GetDefaultAppearanceForClass(string className)
        {
            var appearance = new CharacterAppearance();

            switch (className)
            {
                case "Warrior":
                    appearance.ArmorType = "Plate";
                    appearance.WeaponType = "Longsword";
                    break;
                case "Mage":
                    appearance.ArmorType = "Robe";
                    appearance.WeaponType = "Staff";
                    break;
                case "Rogue":
                    appearance.ArmorType = "Leather";
                    appearance.WeaponType = "Daggers";
                    break;
                default:
                    appearance.ArmorType = "Leather";
                    appearance.WeaponType = "Longsword";
                    break;
            }

            return appearance;
        }
    }
}
