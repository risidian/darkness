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
        private const int ZShield = 130;
        private const int ZWeapon = 140;

        public List<string> HairStyles { get; } = new() { "Long", "Plain", "Curly Long", "Shorthawk", "Spiked", "Bob", "Afro" };
        public List<string> HairColors { get; } = new() { "Blonde", "Black", "Dark Brown", "Redhead", "White", "Gray", "Platinum", "Chestnut", "Blue", "Green", "Purple" };
        public List<string> SkinColors { get; } = new() { "Light", "Amber", "Olive", "Taupe", "Bronze", "Brown", "Black" };
        public List<string> FaceTypes { get; } = new() { "Default", "Female" };
        public List<string> EyeTypes { get; } = new() { "Default", "Neutral", "Anger", "Sad", "Shock" };
        public List<string> HeadTypes { get; } = new() { "Human Male", "Human Female" };
        public List<string> FeetTypes { get; } = new() { "Boots (Basic)", "Boots (Fold)", "Boots (Rimmed)", "Shoes", "Sandals", "None" };
        public List<string> ArmsTypes { get; } = new() { "Gloves", "None" };
        public List<string> LegsTypes { get; } = new() { "Slacks", "Leggings", "Formal", "Cuffed", "Pantaloons", "None" };
        public List<string> ArmorTypes { get; } = new() { "Plate (Steel)", "Plate (Iron)", "Plate (Gold)", "Leather", "Leather (Black)", "Leather (Brown)", "Mage Robes (Blue)", "Mage Robes (Red)", "Mage Robes (White)", "Longsleeve (White)", "Longsleeve (Blue)", "Longsleeve (Brown)" };
        public List<string> WeaponTypes { get; } = new() { "Arming Sword (Steel)", "Arming Sword (Iron)", "Arming Sword (Gold)", "Dagger (Steel)", "Recurve Bow", "Mage Wand", "None" };
        public List<string> ShieldTypes { get; } = new() { "Crusader", "Spartan", "None" };

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
            ["Female"] = "female",
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
            ["Human Female"] = "human_female",
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
            ["Mage Robes (Blue)"] = "blue",
            ["Mage Robes (Red)"] = "red",
            ["Mage Robes (White)"] = "white",
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

        private static readonly Dictionary<string, string> ShieldFileMap = new()
        {
            ["Crusader"] = "crusader",
            ["Spartan"] = "spartan",
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
            var shieldFile = ShieldFileMap.GetValueOrDefault(appearance.ShieldType ?? "None", "");

            // Use original cropped files for preview stability
            var layers = new List<SpriteLayerDefinition>
            {
                new($"sprites/body/{skinFile}.png", ZBody),
                new($"sprites/head/{headFile}.png", ZHead),
                new($"sprites/face/{faceFile}.png", ZFace),
                new($"sprites/eyes/{eyeFile}.png", ZEyes),
                new($"sprites/hair/{hairStyleFile}_{hairColorFile}.png", ZHair),
            };

            if (appearance.ArmorType != null && appearance.ArmorType.StartsWith("Mage Robes"))
            {
                var color = ArmorFileMap[appearance.ArmorType];
                layers.Add(new($"assets/sprites/full/torso/robes/female/{color}/walk.png", ZArmor));
            }
            else
            {
                layers.Add(new($"sprites/armor/{armorFile}.png", ZArmor));
            }

            if (!string.IsNullOrEmpty(feetFile)) layers.Add(new($"sprites/feet/{feetFile}.png", ZFeet));
            if (!string.IsNullOrEmpty(armsFile)) layers.Add(new($"sprites/arms/{armsFile}.png", ZArms));
            if (!string.IsNullOrEmpty(legsFile)) layers.Add(new($"sprites/legs/{legsFile}.png", ZLegs));

            if (!string.IsNullOrEmpty(weaponFile))
            {
                if (weaponFile == "wand")
                    layers.Add(new($"assets/sprites/full/weapons/magic/wand/male/slash/wand.png", ZWeapon));
                else if (weaponFile == "recurve_bow")
                    layers.Add(new($"assets/sprites/full/weapons/ranged/bow/normal/walk/foreground/steel.png", ZWeapon));
                else if (weaponFile == "dagger")
                    layers.Add(new($"assets/sprites/full/weapons/sword/dagger/walk/dagger.png", ZWeapon));
                else
                    layers.Add(new($"sprites/weapons/{weaponFile}.png", ZWeapon));
            }

            if (!string.IsNullOrEmpty(shieldFile))
            {
                layers.Add(new($"assets/sprites/full/shields/{shieldFile}/bg/walk/{shieldFile}.png", ZShield));
            }

            layers.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
            return layers;
        }

        public List<string> GetLayerBasePaths(CharacterAppearance appearance)
        {
            var hairStyleFile = HairStyleFileMap.GetValueOrDefault(appearance.HairStyle ?? "Long", "long");
            var weaponFile = WeaponFileMap.GetValueOrDefault(appearance.WeaponType ?? "Arming Sword (Steel)", "arming_sword_steel");
            var shieldFile = ShieldFileMap.GetValueOrDefault(appearance.ShieldType ?? "None", "");
            var headFile = HeadFileMap.GetValueOrDefault(appearance.Head ?? "Human Male", "human_male");
            var faceFile = FaceFileMap.GetValueOrDefault(appearance.Face ?? "Default", "default");

            var paths = new List<string>
            {
                "assets/sprites/full/body",
                $"assets/sprites/full/hair/{hairStyleFile}/adult/walk"
            };

            if (headFile == "human_female") paths.Add("assets/sprites/full/head/human/female");
            else paths.Add("assets/sprites/full/head/human/male");

            if (faceFile == "female") paths.Add("assets/sprites/full/face/female");
            else paths.Add("assets/sprites/full/face/male");

            if (appearance.ArmorType != null && appearance.ArmorType.StartsWith("Mage Robes"))
            {
                var color = ArmorFileMap[appearance.ArmorType];
                paths.Add($"assets/sprites/full/torso/robes/female/{color}");
            }

            if (weaponFile == "wand") paths.Add("assets/sprites/full/weapons/magic/wand/male/slash");
            else if (weaponFile == "recurve_bow") paths.Add("assets/sprites/full/weapons/ranged/bow/normal/walk/foreground");
            else if (weaponFile == "dagger") paths.Add("assets/sprites/full/weapons/sword/dagger/walk");

            if (!string.IsNullOrEmpty(shieldFile))
            {
                paths.Add($"assets/sprites/full/shields/{shieldFile}/bg/walk");
            }

            return paths;
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
                    appearance.ShieldType = "Crusader";
                    appearance.Head = "Human Male";
                    appearance.Face = "Default";
                    break;
                case "Mage":
                    appearance.ArmorType = "Mage Robes (Blue)";
                    appearance.WeaponType = "Mage Wand";
                    appearance.Feet = "Sandals";
                    appearance.Arms = "None";
                    appearance.Legs = "Formal";
                    appearance.ShieldType = "None";
                    appearance.Head = "Human Female";
                    appearance.Face = "Female";
                    break;
                case "Rogue":
                    appearance.ArmorType = "Leather (Black)";
                    appearance.WeaponType = "Dagger (Steel)";
                    appearance.Feet = "Boots (Fold)";
                    appearance.Arms = "Gloves";
                    appearance.Legs = "Leggings";
                    appearance.ShieldType = "None";
                    appearance.Head = "Human Female";
                    appearance.Face = "Female";
                    break;
                case "Knight":
                    appearance.ArmorType = "Plate (Steel)";
                    appearance.WeaponType = "Arming Sword (Steel)";
                    appearance.Feet = "Boots (Rimmed)";
                    appearance.Arms = "Gloves";
                    appearance.Legs = "Formal";
                    appearance.ShieldType = "Spartan";
                    appearance.Head = "Human Male";
                    appearance.Face = "Default";
                    break;
                case "Cleric":
                    appearance.ArmorType = "Longsleeve (White)";
                    appearance.WeaponType = "Arming Sword (Iron)";
                    appearance.Feet = "Shoes";
                    appearance.Arms = "None";
                    appearance.Legs = "Slacks";
                    appearance.ShieldType = "Crusader";
                    appearance.Head = "Human Female";
                    appearance.Face = "Female";
                    break;
                default:
                    appearance.ArmorType = "Leather";
                    appearance.WeaponType = "Arming Sword (Steel)";
                    appearance.Feet = "Boots (Basic)";
                    appearance.Arms = "None";
                    appearance.Legs = "Slacks";
                    appearance.ShieldType = "None";
                    appearance.Head = "Human Male";
                    appearance.Face = "Default";
                    break;
            }
            return appearance;
        }
    }
}
