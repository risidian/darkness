using System;
using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class SpriteLayerCatalog : ISpriteLayerCatalog
{
    private readonly LiteDatabase _db;
    private readonly Dictionary<string, CharacterAppearance>? _classDefaults;

    public SpriteLayerCatalog(LiteDatabase db, IFileSystemService fileSystem)
    {
        _db = db;
        try
        {
            var json = fileSystem.ReadAllText("assets/data/sprite-catalog.json");
            var data = System.Text.Json.JsonSerializer.Deserialize<SeedWrapper>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            _classDefaults = data?.ClassDefaults;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpriteLayerCatalog] WARN: Could not load class defaults — {ex.Message}");
            _classDefaults = new();
        }
    }

    public List<string> GetOptionNames(string category)
    {
        if (category is "Armor" or "Weapon" or "Shield" or "Feet" or "Legs" or "Arms")
        {
            var col = _db.GetCollection<EquipmentSprite>("equipment_sprites");
            var names = col.Find(s => s.Slot == category).Select(s => s.DisplayName).ToList();
            if (category is "Weapon" or "Shield" or "Feet" or "Arms" or "Legs" && !names.Contains("None"))
                names.Add("None");
            return names;
        }

        var optCol = _db.GetCollection<AppearanceOption>("appearance_options");
        return optCol.Find(o => o.Category == category).Select(o => o.DisplayName).ToList();
    }

    public List<StitchLayer> GetStitchLayers(CharacterAppearance appearance)
    {
        var spriteCol = _db.GetCollection<EquipmentSprite>("equipment_sprites");
        var optionCol = _db.GetCollection<AppearanceOption>("appearance_options");

        var head = appearance.Head ?? "Human Male";
        string gender = head.ToLower().Contains("female") ? "female" : "male";

        var skinOpt = optionCol.FindOne(o => o.Category == "Skin" && o.DisplayName == (appearance.SkinColor ?? "Light"));
        string skinHex = skinOpt?.TintHex ?? "#FFFFFF";

        var layers = new List<(StitchLayer Layer, int Z)>();

        // Body (always present, tinted with skin color)
        layers.Add((new StitchLayer($"assets/sprites/full/body/{gender}", "{action}.png", skinHex), 10));

        // Head
        var headOpt = optionCol.FindOne(o => o.Category == "Head" && o.DisplayName == head);
        if (headOpt != null)
            layers.Add((new StitchLayer(headOpt.AssetPath, headOpt.FileNameTemplate, skinHex), headOpt.ZOrder));

        // Face
        string faceKey = appearance.Face ?? "Default";
        var faceOpt = optionCol.FindOne(o => o.Category == "Face" && o.DisplayName == faceKey);
        if (faceOpt != null)
        {
            string facePath = faceOpt.Gender == "gendered"
                ? faceOpt.AssetPath.Replace("male", gender)
                : faceOpt.AssetPath;
            layers.Add((new StitchLayer(facePath, faceOpt.FileNameTemplate, skinHex), faceOpt.ZOrder));
        }

        // Eyes
        string eyeKey = appearance.Eyes ?? "Default";
        var eyeOpt = optionCol.FindOne(o => o.Category == "Eyes" && o.DisplayName == eyeKey);
        if (eyeOpt != null)
            layers.Add((new StitchLayer(eyeOpt.AssetPath, eyeOpt.FileNameTemplate), eyeOpt.ZOrder));

        // Hair
        string hairKey = appearance.HairStyle ?? "Long";
        var hairOpt = optionCol.FindOne(o => o.Category == "Hair" && o.DisplayName == hairKey);
        if (hairOpt != null)
        {
            string colorName = (appearance.HairColor ?? "Blonde").ToLower().Replace(" ", "_");
            string template = hairOpt.FileNameTemplate.Replace("blonde", colorName);
            // We use #FFFFFF (no tint) because we are using the pre-tinted assets (gray.png, black.png, etc.)
            // which look much better than multiplicative tinting on a blonde base.
            layers.Add((new StitchLayer(hairOpt.AssetPath, template, "#FFFFFF"), hairOpt.ZOrder));
        }

        // Equipment slots
        AddEquipmentLayer(spriteCol, layers, "Armor", appearance.ArmorType ?? "Leather", gender);
        AddEquipmentLayer(spriteCol, layers, "Feet", appearance.Feet ?? "Boots (Basic)", gender);
        AddEquipmentLayer(spriteCol, layers, "Legs", appearance.Legs ?? "Slacks", gender);
        AddEquipmentLayer(spriteCol, layers, "Arms", appearance.Arms ?? "None", gender);
        AddEquipmentLayer(spriteCol, layers, "Weapon", appearance.WeaponType ?? "None", gender);
        AddEquipmentLayer(spriteCol, layers, "Shield", appearance.ShieldType ?? "None", gender);

        return layers.OrderBy(l => l.Z).Select(l => l.Layer).ToList();
    }

    private void AddEquipmentLayer(ILiteCollection<EquipmentSprite> col,
        List<(StitchLayer Layer, int Z)> layers, string slot, string displayName, string gender)
    {
        if (displayName == "None") return;

        var sprite = col.FindOne(s => s.Slot == slot && s.DisplayName == displayName);
        if (sprite == null) return;

        string resolvedGender = sprite.Gender switch
        {
            "gendered" => gender,
            "universal" => "",
            _ => sprite.FallbackGender ?? sprite.Gender
        };

        string basePath = string.IsNullOrEmpty(resolvedGender)
            ? sprite.AssetPath
            : $"{sprite.AssetPath}/{resolvedGender}";

        // LPC weapons/shields have separate bg (behind body) and fg (in front of body) layers.
        // If the asset path ends with /bg, add both bg and fg layers.
        if (basePath.EndsWith("/bg"))
        {
            string fgPath = basePath[..^3] + "/fg";
            layers.Add((new StitchLayer(basePath, sprite.FileNameTemplate, sprite.TintHex), sprite.ZOrder));
            layers.Add((new StitchLayer(fgPath, sprite.FileNameTemplate, sprite.TintHex), sprite.ZOrder + 1));
        }
        else
        {
            layers.Add((new StitchLayer(basePath, sprite.FileNameTemplate, sprite.TintHex), sprite.ZOrder));
        }
    }

    public CharacterAppearance GetDefaultAppearanceForClass(string className)
    {
        if (_classDefaults != null && _classDefaults.TryGetValue(className, out var appearance))
            return appearance;

        return new CharacterAppearance
        {
            ArmorType = "Leather",
            WeaponType = "Arming Sword (Steel)",
            Feet = "Boots (Basic)",
            Arms = "None",
            Legs = "Slacks",
            ShieldType = "None",
            Head = "Human Male",
            Face = "Default"
        };
    }

    private class SeedWrapper
    {
        public Dictionary<string, CharacterAppearance>? ClassDefaults { get; set; }
    }
}
