using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class SheetDefinitionCatalog : ISheetDefinitionCatalog
{
    private readonly ILiteDatabase _db;

    public SheetDefinitionCatalog(ILiteDatabase db)
    {
        _db = db;
    }

    public List<SheetDefinition> GetSheetDefinitions(CharacterAppearance appearance)
    {
        var definitions = new List<SheetDefinition>();
        var col = _db.GetCollection<SheetDefinition>("sheet_definitions");
        var appCol = _db.GetCollection<AppearanceOption>("appearance_options");

        string gender = appearance.Head.ToLower().Contains("female") ? "female" : "male";
        string bodyName = gender == "female" ? "Human Female" : "Human Male";

        // Resolve tints from names
        string skinTint = ResolveTint(appCol, "Skin", appearance.SkinColor);
        string hairTint = ResolveTint(appCol, "HairColor", appearance.HairColor);

        // 1. Body (always present, base for skin color)
        var body = WrapAppearanceOption(appCol.FindOne(x => x.Category == "Body" && x.DisplayName == bodyName), "Body", skinTint);
        if (body != null) definitions.Add(body);

        // 2. Head, Face, Eyes, Hair
        AddAppearanceLayer(definitions, appCol, "Head", appearance.Head, skinTint);
        AddAppearanceLayer(definitions, appCol, "Face", appearance.Face, skinTint);
        AddAppearanceLayer(definitions, appCol, "Eyes", appearance.Eyes, "#FFFFFF"); // Eyes usually not tinted by skin
        AddAppearanceLayer(definitions, appCol, "Hair", appearance.HairStyle, hairTint);

        // 3. Equipment
        AddEquipmentLayer(definitions, col, "Armor", appearance.ArmorType);
        AddEquipmentLayer(definitions, col, "Feet", appearance.Feet);
        AddEquipmentLayer(definitions, col, "Arms", appearance.Arms);
        AddEquipmentLayer(definitions, col, "Legs", appearance.Legs);

        // 4. Weapons
        AddEquipmentLayer(definitions, col, "Weapon", appearance.WeaponType);
        AddEquipmentLayer(definitions, col, "Shield", appearance.ShieldType);
        
        // 5. OffHand
        if (!string.IsNullOrEmpty(appearance.OffHandType) && appearance.OffHandType != "None")
        {
            var offHand = col.FindOne(x => x.Name == appearance.OffHandType);
            if (offHand != null)
            {
                // Clone and flip
                var flipped = new SheetDefinition
                {
                    Id = offHand.Id,
                    Name = offHand.Name,
                    Slot = "OffHand",
                    Layers = offHand.Layers,
                    Variants = offHand.Variants,
                    Animations = offHand.Animations,
                    IsFlipped = true
                };
                definitions.Add(flipped);
            }
        }

        return definitions;
    }

    private string ResolveTint(ILiteCollection<AppearanceOption> col, string category, string displayName)
    {
        if (string.IsNullOrEmpty(displayName) || displayName == "None") return "#FFFFFF";
        var option = col.FindOne(x => x.Category == category && x.DisplayName == displayName);
        return option?.TintHex ?? "#FFFFFF";
    }

    private void AddAppearanceLayer(List<SheetDefinition> list, ILiteCollection<AppearanceOption> col, string category, string displayName, string tint)
    {
        if (displayName == "None") return;
        var option = col.FindOne(x => x.Category == category && x.DisplayName == displayName);
        if (option != null)
        {
            var def = WrapAppearanceOption(option, category, tint);
            if (def != null) list.Add(def);
        }
    }

    private void AddEquipmentLayer(List<SheetDefinition> list, ILiteCollection<SheetDefinition> col, string slot, string displayName)
    {
        if (displayName == "None") return;
        var def = col.FindOne(x => x.Slot == slot && x.Name == displayName);
        if (def != null) list.Add(def);
    }

    private SheetDefinition? WrapAppearanceOption(AppearanceOption? option, string slot, string tint)
    {
        if (option == null) return null;

        // Parse DefaultVariant from FileNameTemplate.
        // "{action}/blonde.png" -> "blonde", "{action}.png" -> null
        string? defaultVariant = null;
        if (!string.IsNullOrEmpty(option.FileNameTemplate) && option.FileNameTemplate.Contains('/'))
        {
            var afterSlash = option.FileNameTemplate.Substring(option.FileNameTemplate.LastIndexOf('/') + 1);
            defaultVariant = Path.GetFileNameWithoutExtension(afterSlash);
        }

        var layer = new SheetLayer
        {
            ZPos = option.ZOrder,
            TintHex = tint,
            DefaultVariant = defaultVariant,
            Paths = new Dictionary<string, string> { { "male", option.AssetPath }, { "female", option.AssetPath } }
        };

        return new SheetDefinition
        {
            Name = option.DisplayName,
            Slot = slot,
            Layers = new Dictionary<string, SheetLayer> { { "base", layer } },
            Animations = new List<string> { "walk", "idle", "slash", "thrust", "shoot", "hurt" } // Default animations
        };
    }

    public CharacterAppearance GetDefaultAppearanceForClass(string className)
    {
        // This should probably be in a seeder/LiteDB too, but for now we can keep it simple
        // or query the database if we have a 'class_defaults' collection.
        // For now, returning a basic one.
        return new CharacterAppearance();
    }

    public List<string> GetOptionNames(string category, string gender)
    {
        var appCol = _db.GetCollection<AppearanceOption>("appearance_options");
        var sheetCol = _db.GetCollection<SheetDefinition>("sheet_definitions");

        var appOptions = appCol.Find(x => x.Category == category)
                  .Where(x => x.Gender == "universal" || x.Gender == gender)
                  .Select(x => x.DisplayName);

        var sheetOptions = sheetCol.Find(x => x.Slot == category)
                  .Select(x => x.Name);

        var result = appOptions.Concat(sheetOptions).ToList();
        if (!result.Contains("None")) result.Insert(0, "None");
        return result;
    }

    public SheetDefinition? GetSheetDefinitionByName(string slot, string displayName)
    {
        var col = _db.GetCollection<SheetDefinition>("sheet_definitions");
        return col.FindOne(x => x.Slot == slot && x.Name == displayName);
    }
}
