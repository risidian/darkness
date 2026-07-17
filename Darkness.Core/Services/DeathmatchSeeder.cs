using System;
using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

public class DeathmatchSeeder
{
    private readonly IFileSystemService _fileSystem;

    public DeathmatchSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(ILiteDatabase db)
    {
        const string jsonPath = "assets/data/deathmatch-encounters.json";

        if (!_fileSystem.FileExists(jsonPath))
        {
            Console.Error.WriteLine($"[DeathmatchSeeder] ERROR: Seed file not found: {jsonPath}");
            return;
        }

        string json;
        try
        {
            json = _fileSystem.ReadAllText(jsonPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DeathmatchSeeder] ERROR: Failed to read {jsonPath} — {ex.Message}");
            return;
        }

        List<DeathmatchEncounter>? encounters;
        try
        {
            encounters = SystemJson.JsonSerializer.Deserialize<List<DeathmatchEncounter>>(json, new SystemJson.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (SystemJson.JsonException ex)
        {
            Console.Error.WriteLine($"[DeathmatchSeeder] ERROR: Failed to parse {jsonPath} — {ex.Message}");
            return;
        }

        if (encounters == null || encounters.Count == 0)
        {
            Console.Error.WriteLine("[DeathmatchSeeder] WARN: deathmatch-encounters.json is empty or null");
            return;
        }

        var col = db.GetCollection<DeathmatchEncounter>("deathmatch_encounters");
        var loadedIds = new List<int>();

        foreach (var encounter in encounters)
        {
            // Keep Name in sync with Title for DeathmatchScene backward compatibility
            if (string.IsNullOrWhiteSpace(encounter.Name) && !string.IsNullOrWhiteSpace(encounter.Title))
                encounter.Name = encounter.Title;

            col.Upsert(encounter);
            loadedIds.Add(encounter.Id);
        }

        // Remove encounters no longer in the JSON (idempotent cleanup)
        col.DeleteMany(x => !loadedIds.Contains(x.Id));

        col.EnsureIndex(e => e.Id);

        Console.WriteLine($"[DeathmatchSeeder] INFO: Synced {col.Count()} deathmatch encounters");
    }
}
