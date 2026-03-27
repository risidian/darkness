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
            var skin = (appearance.SkinColor ?? "Light").ToLower();
            var hairStyle = (appearance.HairStyle ?? "Long").ToLower();
            var hairColor = (appearance.HairColor ?? "Black").ToLower();
            var armor = (appearance.ArmorType ?? "Leather").ToLower();
            var weapon = (appearance.WeaponType ?? "Longsword").ToLower();

            var layers = new List<SpriteLayerDefinition>
            {
                new($"sprites/body/{skin}.png", ZBody),
                new($"sprites/hair/{hairStyle}_{hairColor}.png", ZHair),
                new($"sprites/armor/{armor}.png", ZArmor),
                new($"sprites/weapons/{weapon}.png", ZWeapon),
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
