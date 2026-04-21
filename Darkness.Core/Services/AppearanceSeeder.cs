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
        var loadedOptionIds = new List<int>();
        if (data.AppearanceOptions != null)
        {
            foreach (var option in data.AppearanceOptions)
            {
                if (option.AssetPath.StartsWith("assets/sprites/full/"))
                {
                    option.AssetPath = option.AssetPath.Replace("assets/sprites/full/", "");
                }
                optionCol.Upsert(option);
                loadedOptionIds.Add(option.Id);
            }
        }
        // Cleanup orphaned options
        optionCol.DeleteMany(x => !loadedOptionIds.Contains(x.Id));

        optionCol.EnsureIndex(o => o.Category);
        optionCol.EnsureIndex(o => o.DisplayName);

        Console.WriteLine($"[AppearanceSeeder] INFO: Synced {optionCol.Count()} appearance options");

        var defaultsCol = db.GetCollection<ClassDefault>("class_defaults");
        var loadedDefaultNames = new List<string>();
        if (data.ClassDefaults != null)
        {
            foreach (var kvp in data.ClassDefaults)
            {
                var cd = kvp.Value;
                cd.ClassName = kvp.Key;
                defaultsCol.Upsert(cd);
                loadedDefaultNames.Add(cd.ClassName);
            }
        }
        // Cleanup orphaned class defaults
        defaultsCol.DeleteMany(x => !loadedDefaultNames.Contains(x.ClassName));

        defaultsCol.EnsureIndex(c => c.ClassName);

        Console.WriteLine($"[AppearanceSeeder] INFO: Loaded {defaultsCol.Count()} class defaults");
    }

    private class SeedData
    {
        public List<AppearanceOption>? AppearanceOptions { get; set; }
        public Dictionary<string, ClassDefault>? ClassDefaults { get; set; }
    }
}
