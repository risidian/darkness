using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Collections.Generic;
using System.Linq;

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

        public List<string> HairStyles { get; } = new()
            { "Long", "Plain", "Curly Long", "Shorthawk", "Spiked", "Bob", "Afro" };

        public List<string> HairColors { get; } = new()
        {
            "Blonde", "Black", "Dark Brown", "Redhead", "White", "Gray", "Platinum", "Chestnut", "Blue", "Green",
            "Purple"
        };

        public List<string> SkinColors { get; } =
            new() { "Light", "Amber", "Olive", "Taupe", "Bronze", "Brown", "Black" };

        public List<string> FaceTypes { get; } = new() { "Default", "Female" };
        public List<string> EyeTypes { get; } = new() { "Default", "Neutral", "Anger", "Sad", "Shock" };
        public List<string> HeadTypes { get; } = new() { "Human Male", "Human Female" };

        public List<string> FeetTypes { get; } = new()
            { "Boots (Basic)", "Boots (Fold)", "Boots (Rimmed)", "Shoes", "Sandals", "None" };

        public List<string> ArmsTypes { get; } = new() { "Gloves", "None" };

        public List<string> LegsTypes { get; } =
            new() { "Slacks", "Leggings", "Formal", "Cuffed", "Pantaloons", "None" };

        public List<string> ArmorTypes { get; } = new()
        {
            "Plate (Steel)", "Plate (Iron)", "Plate (Gold)", "Leather", "Leather (Black)", "Leather (Brown)",
            "Mage Robes (Blue)", "Mage Robes (Red)", "Mage Robes (White)", "Longsleeve (White)", "Longsleeve (Blue)",
            "Longsleeve (Brown)"
        };

        public List<string> WeaponTypes { get; } = new()
        {
            "Arming Sword (Steel)", "Arming Sword (Iron)", "Arming Sword (Gold)", "Dagger (Steel)", "Recurve Bow",
            "Mage Wand", "None"
        };

        public List<string> ShieldTypes { get; } = new() { "Crusader", "Spartan", "None" };

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

        private static readonly Dictionary<string, string> SkinColorHexMap = new()
        {
            ["Light"] = "#FFFFFF",
            ["Amber"] = "#E0AC69",
            ["Olive"] = "#C68642",
            ["Taupe"] = "#8D5524",
            ["Bronze"] = "#754C24",
            ["Brown"] = "#4B3018",
            ["Black"] = "#2D1B0F",
        };

        private static readonly Dictionary<string, string> HairColorHexMap = new()
        {
            ["Blonde"] = "#FFFFFF",
            ["Black"] = "#090806",
            ["Dark Brown"] = "#3B3024",
            ["Redhead"] = "#A52A2A",
            ["White"] = "#EAEAEA",
            ["Gray"] = "#808080",
            ["Platinum"] = "#E5E4E2",
            ["Chestnut"] = "#954535",
            ["Blue"] = "#0000FF",
            ["Green"] = "#00FF00",
            ["Purple"] = "#800080",
        };

        private static readonly Dictionary<string, string> ArmorFileMap = new()
        {
            ["Plate (Steel)"] = "plate/steel",
            ["Plate (Iron)"] = "plate/iron",
            ["Plate (Gold)"] = "plate/gold",
            ["Leather"] = "leather/leather",
            ["Leather (Black)"] = "leather/black",
            ["Leather (Brown)"] = "leather/brown",
            ["Mage Robes (Blue)"] = "blue",
            ["Mage Robes (Red)"] = "red",
            ["Mage Robes (White)"] = "white",
            ["Longsleeve (White)"] = "longsleeve_white",
            ["Longsleeve (Blue)"] = "longsleeve_blue",
            ["Longsleeve (Brown)"] = "longsleeve_brown",
        };

        private static readonly Dictionary<string, string> FeetFileMap = new()
        {
            ["Boots (Basic)"] = "shoes/basic",
            ["Boots (Fold)"] = "shoes/basic",
            ["Boots (Rimmed)"] = "shoes/basic",
            ["Shoes"] = "shoes/basic",
            ["Sandals"] = "shoes/basic",
            ["None"] = "",
        };

        private static readonly Dictionary<string, string> ArmsFileMap = new()
        {
            ["Gloves"] = "gloves",
            ["None"] = "",
        };

        private static readonly Dictionary<string, string> LegsFileMap = new()
        {
            ["Slacks"] = "pants",
            ["Leggings"] = "leggings",
            ["Formal"] = "formal",
            ["Cuffed"] = "cuffed",
            ["Pantaloons"] = "pantaloons",
            ["None"] = "",
        };

        private static readonly Dictionary<string, string> LegacyArmorFileMap = new()
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

        private static readonly Dictionary<string, string> LegacyFeetFileMap = new()
        {
            ["Boots (Basic)"] = "boots_basic",
            ["Boots (Fold)"] = "boots_fold",
            ["Boots (Rimmed)"] = "boots_rimmed",
            ["Shoes"] = "shoes",
            ["Sandals"] = "sandals",
            ["None"] = "",
        };

        private static readonly Dictionary<string, string> LegacyArmsFileMap = new()
        {
            ["Gloves"] = "gloves",
            ["None"] = "",
        };

        private static readonly Dictionary<string, string> LegacyLegsFileMap = new()
        {
            ["Slacks"] = "slacks",
            ["Leggings"] = "leggings",
            ["Formal"] = "formal",
            ["Cuffed"] = "cuffed",
            ["Pantaloons"] = "pantaloons",
            ["None"] = "",
        };

        public List<SpriteLayerDefinition> GetLayersForAppearance(CharacterAppearance appearance)
        {
            var skin = appearance.SkinColor ?? "Light";
            var hairStyle = (appearance.HairStyle ?? "Long").ToLower().Replace(" ", "_");
            var hairColor = (appearance.HairColor ?? "Black").ToLower().Replace(" ", "_");
            var head = appearance.Head ?? "Human Male";
            var armor = appearance.ArmorType ?? "Leather";
            var feet = appearance.Feet ?? "Boots (Basic)";
            var arms = appearance.Arms ?? "None";
            var legs = appearance.Legs ?? "Slacks";

            var layers = new List<SpriteLayerDefinition>
            {
                new($"sprites/body/{skin.ToLower()}.png", ZBody),
                new($"sprites/head/human_{head.ToLower().Split(' ')[1]}.png", ZHead),
                new($"sprites/face/default.png", ZFace),
                new($"sprites/eyes/default.png", ZEyes),
                new($"sprites/hair/{hairStyle}_{hairColor}.png", ZHair),
            };

            if (armor.StartsWith("Mage Robes"))
            {
                var color = LegacyArmorFileMap.GetValueOrDefault(armor, "blue");
                layers.Add(new($"assets/sprites/full/torso/robes/female/{color}/walk.png", ZArmor));
            }
            else
            {
                string file = LegacyArmorFileMap.GetValueOrDefault(armor, "leather_leather");
                layers.Add(new($"sprites/armor/{file}.png", ZArmor));
            }

            if (!string.IsNullOrEmpty(feet) && feet != "None")
                layers.Add(new($"sprites/feet/{LegacyFeetFileMap.GetValueOrDefault(feet, "boots_basic")}.png", ZFeet));
            if (!string.IsNullOrEmpty(arms) && arms != "None")
                layers.Add(new($"sprites/arms/{LegacyArmsFileMap.GetValueOrDefault(arms, "gloves")}.png", ZArms));
            if (!string.IsNullOrEmpty(legs) && legs != "None")
                layers.Add(new($"sprites/legs/{LegacyLegsFileMap.GetValueOrDefault(legs, "slacks")}.png", ZLegs));

            layers.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
            return layers;
        }

        public List<StitchLayer> GetStitchLayers(CharacterAppearance appearance)
        {
            var skinHex = SkinColorHexMap.GetValueOrDefault(appearance.SkinColor ?? "Light", "#FFFFFF");
            var hairHex = HairColorHexMap.GetValueOrDefault(appearance.HairColor ?? "Black", "#FFFFFF");
            var hairStyle = HairStyleFileMap.GetValueOrDefault(appearance.HairStyle ?? "Long", "long");
            var head = appearance.Head ?? "Human Male";
            var armor = appearance.ArmorType ?? "Leather";
            var weapon = appearance.WeaponType ?? "None";
            var shield = appearance.ShieldType ?? "None";
            var feet = appearance.Feet ?? "Boots (Basic)";
            var arms = appearance.Arms ?? "None";
            var legs = appearance.Legs ?? "Slacks";

            var layers = new List<(StitchLayer Layer, int Z)>();
            string gender = head.ToLower().Contains("female") ? "female" : "male";

            // Body
            layers.Add((new StitchLayer($"assets/sprites/full/body/{gender}", "{action}.png", skinHex), ZBody));

            // Head
            layers.Add((new StitchLayer($"assets/sprites/full/head/human/{gender}", "{action}.png", skinHex), ZHead));

            // Face
            string faceGender = (appearance.Face == "Female") ? "female" : "male";
            layers.Add((new StitchLayer($"assets/sprites/full/face/{faceGender}", "{action}.png", skinHex), ZFace));

            // Eyes
            string eyeExpr = (appearance.Eyes ?? "Default").ToLower();
            layers.Add((new StitchLayer($"assets/sprites/full/eyes/human/adult/{eyeExpr}", "{action}/blue.png"),
                ZEyes));

            // Armor/Robes
            if (armor.StartsWith("Mage Robes"))
            {
                var color = ArmorFileMap[armor];
                layers.Add((new StitchLayer($"assets/sprites/full/torso/robes/female/{color}", "{action}.png"),
                    ZArmor));
            }
            else if (ArmorFileMap.TryGetValue(armor, out var armorInfo))
            {
                var parts = armorInfo.Split('/');
                if (parts.Length == 2)
                {
                    layers.Add((
                        new StitchLayer($"assets/sprites/full/armor/{parts[0]}/{gender}", $"{{action}}/{parts[1]}.png"),
                        ZArmor));
                }
            }

            // Legs
            if (legs != "None" && LegsFileMap.TryGetValue(legs, out var legsFile))
            {
                layers.Add((new StitchLayer($"assets/sprites/full/legs/{legsFile}/{gender}", "{action}/black.png"),
                    ZLegs));
            }

            // Feet
            if (feet != "None" && FeetFileMap.TryGetValue(feet, out var feetFile))
            {
                layers.Add((new StitchLayer($"assets/sprites/full/feet/{feetFile}/{gender}", "{action}/black.png"),
                    ZFeet));
            }

            // Arms (Gloves)
            if (arms != "None" && ArmsFileMap.TryGetValue(arms, out var armsFile))
            {
                layers.Add((new StitchLayer($"assets/sprites/full/arms/{armsFile}/{gender}", "{action}/black.png"),
                    ZArms));
            }

            // Hair
            layers.Add((new StitchLayer($"assets/sprites/full/hair/{hairStyle}/adult", "{action}/blonde.png", hairHex),
                ZHair));

            // Weapons
            if (weapon.Contains("Wand"))
                layers.Add((new StitchLayer("assets/sprites/full/weapons/magic/wand/male/slash", "wand.png"), ZWeapon));
            else if (weapon.Contains("Bow"))
                layers.Add((
                    new StitchLayer("assets/sprites/full/weapons/ranged/bow/normal/walk/foreground", "steel.png"),
                    ZWeapon));
            else if (weapon.Contains("Dagger"))
                layers.Add((new StitchLayer("assets/sprites/full/weapons/sword/dagger/walk", "dagger.png"), ZWeapon));

            // Shield
            if (shield != "None" && !string.IsNullOrEmpty(shield))
            {
                layers.Add((
                    new StitchLayer($"assets/sprites/full/shields/{shield.ToLower()}/bg/walk",
                        $"{shield.ToLower()}.png"), ZShield));
            }

            // Sort by Z and return just the layers
            return layers.OrderBy(l => l.Z).Select(l => l.Layer).ToList();
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
                    appearance.Head = "Human Male";
                    appearance.Face = "Default";
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