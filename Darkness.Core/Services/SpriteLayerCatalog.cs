using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services
{
    public class SpriteLayerCatalog : ISpriteLayerCatalog
    {
        // Z-order from LPC sheet definitions
        private const int ZBody = 10;
        private const int ZFeet = 15;
        private const int ZLegs = 40;
        private const int ZArms = 55;
        private const int ZArmor = 60;
        private const int ZHead = 90;
        private const int ZFace = 100;
        private const int ZEyes = 105;
        private const int ZHair = 120;
        private const int ZWeapon = 140;

        public List<string> HairStyles { get; } = new() { "Long", "Plain", "Curly Long", "Shorthawk", "Spiked", "Bob", "Afro" };
        public List<string> HairColors { get; } = new() { "Blonde", "Black", "Dark Brown", "Redhead", "White", "Gray", "Platinum", "Chestnut", "Blue", "Green", "Purple" };
        public List<string> SkinColors { get; } = new() { "Light", "Amber", "Olive", "Taupe", "Bronze", "Brown", "Black" };
        public List<string> FaceTypes { get; } = new() { "Default" };
        public List<string> EyeTypes { get; } = new() { "Default", "Neutral", "Anger", "Sad", "Shock" };
        public List<string> HeadTypes { get; } = new() { "Human Male" };
        public List<string> FeetTypes { get; } = new() { "Boots (Basic)", "Boots (Fold)", "Boots (Rimmed)", "Shoes", "Sandals", "None" };
        public List<string> ArmsTypes { get; } = new() { "Gloves", "None" };
        public List<string> LegsTypes { get; } = new() { "Slacks", "Leggings", "Formal", "Cuffed", "Pantaloons", "None" };
        public List<string> ArmorTypes { get; } = new() { "Plate (Steel)", "Plate (Iron)", "Plate (Gold)", "Leather", "Leather (Black)", "Leather (Brown)", "Longsleeve (White)", "Longsleeve (Blue)", "Longsleeve (Brown)" };
        public List<string> WeaponTypes { get; } = new() { "Arming Sword (Steel)", "Arming Sword (Iron)", "Arming Sword (Gold)", "Dagger (Steel)", "Recurve Bow", "Mage Wand", "None" };

        // Maps display name → file name fragment
        private static readonly Dictionary<string, string> HairStyleFileMap = new()
        {
            ["Long"] = "long",
            ["Plain"] = "plain",
            ["Curly Long"] = "curly_long",
            ["Shorthawk"] = "shorthawk",
            ["Spiked"] = "spiked",
            ["Bob"] = "bob",
            ["Afro"] = "afro",
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
            ["Blue"] = "blue",
            ["Green"] = "green",
            ["Purple"] = "purple",
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

        private static readonly Dictionary<string, string> FaceFileMap = new()
        {
            ["Default"] = "default",
        };

        private static readonly Dictionary<string, string> EyeFileMap = new()
        {
            ["Default"] = "default",
            ["Neutral"] = "neutral",
            ["Anger"] = "anger",
            ["Sad"] = "sad",
            ["Shock"] = "shock",
        };

        private static readonly Dictionary<string, string> HeadFileMap = new()
        {
            ["Human Male"] = "human_male",
        };

        private static readonly Dictionary<string, string> FeetFileMap = new()
        {
            ["Boots (Basic)"] = "boots_basic",
            ["Boots (Fold)"] = "boots_fold",
            ["Boots (Rimmed)"] = "boots_rimmed",
            ["Shoes"] = "shoes",
            ["Sandals"] = "sandals",
            ["None"] = "",
        };

        private static readonly Dictionary<string, string> ArmsFileMap = new()
        {
            ["Gloves"] = "gloves",
            ["None"] = "",
        };

        private static readonly Dictionary<string, string> LegsFileMap = new()
        {
            ["Slacks"] = "slacks",
            ["Leggings"] = "leggings",
            ["Formal"] = "formal",
            ["Cuffed"] = "cuffed",
            ["Pantaloons"] = "pantaloons",
            ["None"] = "",
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
            ["Dagger (Steel)"] = "dagger",
            ["Recurve Bow"] = "recurve_bow",
            ["Mage Wand"] = "wand",
            ["None"] = "",
        };

        public List<SpriteLayerDefinition> GetLayersForAppearance(CharacterAppearance appearance)
        {
            var skinFile = SkinColorFileMap.GetValueOrDefault(appearance.SkinColor ?? "Light", "light");
            var hairStyleFile = HairStyleFileMap.GetValueOrDefault(appearance.HairStyle ?? "Long", "long");
            var hairColorFile = HairColorFileMap.GetValueOrDefault(appearance.HairColor ?? "Black", "black");
            var faceFile = FaceFileMap.GetValueOrDefault(appearance.Face ?? "Default", "default");
            var eyeFile = EyeFileMap.GetValueOrDefault(appearance.Eyes ?? "Default", "default");
            var headFile = HeadFileMap.GetValueOrDefault(appearance.Head ?? "Human Male", "human_male");
            var feetFile = FeetFileMap.GetValueOrDefault(appearance.Feet ?? "Boots (Basic)", "boots_basic");
            var armsFile = ArmsFileMap.GetValueOrDefault(appearance.Arms ?? "None", "");
            var legsFile = LegsFileMap.GetValueOrDefault(appearance.Legs ?? "Slacks", "slacks");
            var armorFile = ArmorFileMap.GetValueOrDefault(appearance.ArmorType ?? "Leather", "leather_leather");
            var weaponFile = WeaponFileMap.GetValueOrDefault(appearance.WeaponType ?? "Arming Sword (Steel)", "arming_sword_steel");

            var layers = new List<SpriteLayerDefinition>
            {
                new($"assets/sprites/full/body/walk.png", ZBody),
                new($"assets/sprites/full/body/walk.png", ZHead),
                new($"assets/sprites/full/body/walk.png", ZFace),
                new($"assets/sprites/full/body/walk.png", ZEyes),
                new($"assets/sprites/full/body/walk.png", ZHair),
                new($"assets/sprites/full/body/walk.png", ZArmor),
            };

            if (!string.IsNullOrEmpty(feetFile))
            {
                layers.Add(new($"assets/sprites/full/body/walk.png", ZFeet));
            }

            if (!string.IsNullOrEmpty(armsFile))
            {
                layers.Add(new($"assets/sprites/full/body/walk.png", ZArms));
            }

            if (!string.IsNullOrEmpty(legsFile))
            {
                layers.Add(new($"assets/sprites/full/body/walk.png", ZLegs));
            }

            if (!string.IsNullOrEmpty(weaponFile))
            {
                // Simple mapping for now
                if (weaponFile == "wand")
                    layers.Add(new($"assets/sprites/full/weapons/magic/wand/male/slash/wand.png", ZWeapon));
                else if (weaponFile == "recurve_bow")
                    layers.Add(new($"assets/sprites/full/weapons/ranged/bow/male/shoot/recurve_bow.png", ZWeapon));
                else if (weaponFile == "dagger")
                    layers.Add(new($"assets/sprites/full/weapons/sword/dagger/male/slash/dagger.png", ZWeapon));
                else
                    layers.Add(new($"assets/sprites/full/body/walk.png", ZWeapon));
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
                    appearance.Feet = "Boots (Basic)";
                    appearance.Arms = "Gloves";
                    appearance.Legs = "Slacks";
                    break;
                case "Mage":
                    appearance.ArmorType = "Longsleeve (Blue)";
                    appearance.WeaponType = "Mage Wand";
                    appearance.Feet = "Sandals";
                    appearance.Arms = "None";
                    appearance.Legs = "Formal";
                    break;
                case "Rogue":
                    appearance.ArmorType = "Leather (Black)";
                    appearance.WeaponType = "Dagger (Steel)";
                    appearance.Feet = "Boots (Fold)";
                    appearance.Arms = "Gloves";
                    appearance.Legs = "Leggings";
                    break;
                case "Knight":
                    appearance.ArmorType = "Plate (Steel)";
                    appearance.WeaponType = "Arming Sword (Steel)";
                    appearance.Feet = "Boots (Rimmed)";
                    appearance.Arms = "Gloves";
                    appearance.Legs = "Formal";
                    break;
                case "Cleric":
                    appearance.ArmorType = "Longsleeve (White)";
                    appearance.WeaponType = "Arming Sword (Iron)";
                    appearance.Feet = "Shoes";
                    appearance.Arms = "None";
                    appearance.Legs = "Slacks";
                    break;
                default:
                    appearance.ArmorType = "Leather";
                    appearance.WeaponType = "Arming Sword (Steel)";
                    appearance.Feet = "Boots (Basic)";
                    appearance.Arms = "None";
                    appearance.Legs = "Slacks";
                    break;
            }

            return appearance;
        }
    }
}
