using System;
using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

public class ItemSeeder
{
    private readonly IFileSystemService _fileSystem;

    public ItemSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(ILiteDatabase db)
    {
        string json;
        try
        {
            json = _fileSystem.ReadAllText("assets/data/items.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ItemSeeder] ERROR: Failed to read items.json — {ex.Message}");
            return;
        }

        List<Item>? items;
        try
        {
            items = SystemJson.JsonSerializer.Deserialize<List<Item>>(json, new SystemJson.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (SystemJson.JsonException ex)
        {
            Console.WriteLine($"[ItemSeeder] ERROR: Failed to parse items.json — {ex.Message}");
            return;
        }

        if (items == null || items.Count == 0)
        {
            Console.WriteLine("[ItemSeeder] WARN: items.json is empty or null");
            return;
        }

        var col = db.GetCollection<Item>("items");
        col.DeleteAll();
        foreach (var item in items)
            col.Insert(item);
        col.EnsureIndex(i => i.Name);

        Console.WriteLine($"[ItemSeeder] INFO: Loaded {col.Count()} items");
    }
}
