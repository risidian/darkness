using System;
using System.Collections.Generic;
using SystemJson = System.Text.Json;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class SpriteSeeder
{
    private readonly IFileSystemService _fileSystem;

    public SpriteSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(LiteDatabase db)
    {
        string json;
        try
        {
            json = _fileSystem.ReadAllText("assets/data/sprite-catalog.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpriteSeeder] ERROR: Failed to read sprite-catalog.json — {ex.Message}");
            return;
        }

        SeedData? data;
        try
        {
            data = SystemJson.JsonSerializer.Deserialize<SeedData>(json, new SystemJson.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (SystemJson.JsonException ex)
        {
            Console.WriteLine($"[SpriteSeeder] ERROR: Failed to parse sprite-catalog.json — {ex.Message}");
            return;
        }

        if (data == null)
        {
            Console.WriteLine("[SpriteSeeder] ERROR: sprite-catalog.json deserialized to null");
            return;
        }

        var spriteCol = db.GetCollection<EquipmentSprite>("equipment_sprites");
        spriteCol.DeleteAll();
        if (data.EquipmentSprites != null)
        {
            foreach (var sprite in data.EquipmentSprites)
                spriteCol.Insert(sprite);
        }
        spriteCol.EnsureIndex(s => s.Slot);
        spriteCol.EnsureIndex(s => s.DisplayName);

        var optionCol = db.GetCollection<AppearanceOption>("appearance_options");
        optionCol.DeleteAll();
        if (data.AppearanceOptions != null)
        {
            foreach (var option in data.AppearanceOptions)
                optionCol.Insert(option);
        }
        optionCol.EnsureIndex(o => o.Category);
        optionCol.EnsureIndex(o => o.DisplayName);

        Console.WriteLine($"[SpriteSeeder] INFO: Loaded {spriteCol.Count()} equipment sprites and {optionCol.Count()} appearance options");
    }

    private class SeedData
    {
        public List<EquipmentSprite>? EquipmentSprites { get; set; }
        public List<AppearanceOption>? AppearanceOptions { get; set; }
        public Dictionary<string, CharacterAppearance>? ClassDefaults { get; set; }
    }
}
