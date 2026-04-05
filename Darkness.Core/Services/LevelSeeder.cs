using System;
using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

public class LevelSeeder
{
    private readonly IFileSystemService _fileSystem;

    public LevelSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(LiteDatabase db)
    {
        string json;
        try
        {
            json = _fileSystem.ReadAllText("assets/data/level-table.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LevelSeeder] ERROR: Failed to read level-table.json — {ex.Message}");
            return;
        }

        List<Level>? levels;
        try
        {
            levels = SystemJson.JsonSerializer.Deserialize<List<Level>>(json, new SystemJson.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (SystemJson.JsonException ex)
        {
            Console.WriteLine($"[LevelSeeder] ERROR: Failed to parse level-table.json — {ex.Message}");
            return;
        }

        if (levels == null || levels.Count == 0)
        {
            Console.WriteLine("[LevelSeeder] WARN: level-table.json is empty or null");
            return;
        }

        var col = db.GetCollection<Level>("levels");
        col.DeleteAll();
        foreach (var level in levels)
            col.Insert(level);
        col.EnsureIndex(l => l.Value);

        Console.WriteLine($"[LevelSeeder] INFO: Loaded {col.Count()} level thresholds (max level: {levels[^1].Value})");
    }
}
