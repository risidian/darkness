using System;
using System.Collections.Generic;
using SystemJson = System.Text.Json;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class AppearanceSeeder
{
    private readonly IFileSystemService _fileSystem;

    public AppearanceSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(ILiteDatabase db)
    {
        string json;
        try
        {
            json = _fileSystem.ReadAllText("assets/data/sprite-catalog.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppearanceSeeder] ERROR: Failed to read sprite-catalog.json — {ex.Message}");
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
            Console.WriteLine($"[AppearanceSeeder] ERROR: Failed to parse sprite-catalog.json — {ex.Message}");
            return;
        }

        if (data == null)
        {
            Console.WriteLine("[AppearanceSeeder] ERROR: sprite-catalog.json deserialized to null");
            return;
        }

        var optionCol = db.GetCollection<AppearanceOption>("appearance_options");
        optionCol.DeleteAll();
        if (data.AppearanceOptions != null)
        {
            foreach (var option in data.AppearanceOptions)
                optionCol.Insert(option);
        }
        optionCol.EnsureIndex(o => o.Category);
        optionCol.EnsureIndex(o => o.DisplayName);

        Console.WriteLine($"[AppearanceSeeder] INFO: Loaded {optionCol.Count()} appearance options");
    }

    private class SeedData
    {
        public List<AppearanceOption>? AppearanceOptions { get; set; }
    }
}
