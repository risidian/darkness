using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services
{
    public class SpriteLayerCatalog : ISpriteLayerCatalog
    {
        // Z-order from LPC sheet definitions
        private const int ZBody = 10;
        private const int ZArmor = 60;
        private const int ZHair = 120;
        private const int ZWeapon = 140;

        public List<string> HairStyles { get; } = new() { "Long", "Plain", "Curly Long", "Shorthawk", "Spiked", "Bob" };
        public List<string> HairColors { get; } = new() { "Blonde", "Black", "Dark Brown", "Redhead", "White", "Gray", "Platinum", "Chestnut" };
        public List<string> SkinColors { get; } = new() { "Light", "Amber", "Olive", "Taupe", "Bronze", "Brown", "Black" };
        public List<string> ArmorTypes { get; } = new() { "Plate (Steel)", "Plate (Iron)", "Plate (Gold)", "Leather", "Leather (Black)", "Leather (Brown)", "Longsleeve (White)", "Longsleeve (Blue)", "Longsleeve (Brown)" };
        public List<string> WeaponTypes { get; } = new() { "Arming Sword (Steel)", "Arming Sword (Iron)", "Arming Sword (Gold)", "None" };

        // Maps display name → file name fragment
        private static readonly Dictionary<string, string> HairStyleFileMap = new()
        {
            ["Long"] = "long",
            ["Plain"] = "plain",
            ["Curly Long"] = "curly_long",
            ["Shorthawk"] = "shorthawk",
            ["Spiked"] = "spiked",
            ["Bob"] = "bob",
        };

        private static readonly Dictionary<string, string> HairColorFileMap = new()
        {
            ["Blonde"] = "blonde",
            ["Black"] = "black",
            ["Dark Brown"] = "dark_brown",
            ["Redhead"] = "redhead",
            ["White"] = "white",
            ["Gray"] = "gray",
            ["Platinum"] = "platinum",
            ["Chestnut"] = "chestnut",
        };

        private static readonly Dictionary<string, string> SkinColorFileMap = new()
        {
            ["Light"] = "light",
            ["Amber"] = "amber",
            ["Olive"] = "olive",
            ["Taupe"] = "taupe",
            ["Bronze"] = "bronze",
            ["Brown"] = "brown",
            ["Black"] = "black",
        };

        private static readonly Dictionary<string, string> ArmorFileMap = new()
        {
            ["Plate (Steel)"] = "plate_steel",
            ["Plate (Iron)"] = "plate_iron",
            ["Plate (Gold)"] = "plate_gold",
            ["Leather"] = "leather_leather",
            ["Leather (Black)"] = "leather_black",
            ["Leather (Brown)"] = "leather_brown",
            ["Longsleeve (White)"] = "longsleeve_white",
            ["Longsleeve (Blue)"] = "longsleeve_blue",
            ["Longsleeve (Brown)"] = "longsleeve_brown",
        };

        private static readonly Dictionary<string, string> WeaponFileMap = new()
        {
            ["Arming Sword (Steel)"] = "arming_sword_steel",
            ["Arming Sword (Iron)"] = "arming_sword_iron",
            ["Arming Sword (Gold)"] = "arming_sword_gold",
            ["None"] = "",
        };

        public List<SpriteLayerDefinition> GetLayersForAppearance(CharacterAppearance appearance)
        {
            var skinFile = SkinColorFileMap.GetValueOrDefault(appearance.SkinColor ?? "Light", "light");
            var hairStyleFile = HairStyleFileMap.GetValueOrDefault(appearance.HairStyle ?? "Long", "long");
            var hairColorFile = HairColorFileMap.GetValueOrDefault(appearance.HairColor ?? "Black", "black");
            var armorFile = ArmorFileMap.GetValueOrDefault(appearance.ArmorType ?? "Leather", "leather_leather");
            var weaponFile = WeaponFileMap.GetValueOrDefault(appearance.WeaponType ?? "Arming Sword (Steel)", "arming_sword_steel");

            var layers = new List<SpriteLayerDefinition>
            {
                new($"sprites/body/{skinFile}.png", ZBody),
                new($"sprites/hair/{hairStyleFile}_{hairColorFile}.png", ZHair),
                new($"sprites/armor/{armorFile}.png", ZArmor),
            };

            if (!string.IsNullOrEmpty(weaponFile))
            {
                layers.Add(new($"sprites/weapons/{weaponFile}.png", ZWeapon));
            }

            layers.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
            return layers;
        }

        public CharacterAppearance GetDefaultAppearanceForClass(string className)
        {
            var appearance = new CharacterAppearance();

            switch (className)
            {
                case "Warrior":
                    appearance.ArmorType = "Plate (Steel)";
                    appearance.WeaponType = "Arming Sword (Steel)";
                    break;
                case "Mage":
                    appearance.ArmorType = "Longsleeve (Blue)";
                    appearance.WeaponType = "None";
                    break;
                case "Rogue":
                    appearance.ArmorType = "Leather (Black)";
                    appearance.WeaponType = "Arming Sword (Iron)";
                    break;
                default:
                    appearance.ArmorType = "Leather";
                    appearance.WeaponType = "Arming Sword (Steel)";
                    break;
            }

            return appearance;
        }
    }
}
