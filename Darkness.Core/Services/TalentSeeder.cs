using System;
using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

public class TalentSeeder
{
    private readonly IFileSystemService _fileSystem;

    public TalentSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(LiteDatabase db)
    {
        const string jsonPath = "assets/data/talent-trees.json";
        
        if (!_fileSystem.FileExists(jsonPath))
        {
            Console.Error.WriteLine($"[TalentSeeder] ERROR: File not found: {jsonPath}");
            return;
        }

        string json;
        try
        {
            json = _fileSystem.ReadAllText(jsonPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[TalentSeeder] ERROR: Failed to read talent-trees.json — {ex.Message}");
            return;
        }

        List<TalentTree>? trees;
        try
        {
            trees = SystemJson.JsonSerializer.Deserialize<List<TalentTree>>(json, new SystemJson.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (SystemJson.JsonException ex)
        {
            Console.Error.WriteLine($"[TalentSeeder] ERROR: Failed to parse talent-trees.json — {ex.Message}");
            return;
        }

        if (trees == null || trees.Count == 0)
        {
            Console.Error.WriteLine("[TalentSeeder] WARN: talent-trees.json is empty or null");
            return;
        }

        var col = db.GetCollection<TalentTree>("talent_trees");
        col.DeleteAll();
        col.InsertBulk(trees);
        col.EnsureIndex(t => t.Id);

        Console.Error.WriteLine($"[TalentSeeder] INFO: Loaded {col.Count()} talent trees");
    }
}
