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
        string json;
        try
        {
            json = _fileSystem.ReadAllText("assets/data/talent-trees.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TalentSeeder] ERROR: Failed to read talent-trees.json — {ex.Message}");
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
            Console.WriteLine($"[TalentSeeder] ERROR: Failed to parse talent-trees.json — {ex.Message}");
            return;
        }

        if (trees == null || trees.Count == 0)
        {
            Console.WriteLine("[TalentSeeder] WARN: talent-trees.json is empty or null");
            return;
        }

        var col = db.GetCollection<TalentTree>("talent_trees");
        col.DeleteAll();
        col.InsertBulk(trees);
        col.EnsureIndex(t => t.Id);

        Console.WriteLine($"[TalentSeeder] INFO: Loaded {col.Count()} talent trees");
    }
}
