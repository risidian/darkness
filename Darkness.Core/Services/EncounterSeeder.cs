using System;
using System.Collections.Generic;
using System.IO;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

public class EncounterSeeder
{
    private readonly IFileSystemService _fileSystem;

    public EncounterSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(ILiteDatabase db)
    {
        const string jsonPath = "assets/data/encounters.json";

        if (!_fileSystem.FileExists(jsonPath))
        {
            Console.Error.WriteLine($"[EncounterSeeder] ERROR: Seed file not found: {jsonPath}");
            return;
        }

        var col = db.GetCollection<EncounterTable>("encounter_tables");

        try
        {
            var json = _fileSystem.ReadAllText(jsonPath);
            var tables = SystemJson.JsonSerializer.Deserialize<List<EncounterTable>>(json, new SystemJson.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tables != null)
            {
                col.DeleteAll();
                col.InsertBulk(tables);
                Console.Error.WriteLine($"[EncounterSeeder] INFO: Loaded {tables.Count} encounter tables");
            }
            else
            {
                Console.Error.WriteLine("[EncounterSeeder] WARN: encounters.json is empty or null");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EncounterSeeder] ERROR: Failed to seed encounters — {ex.Message}");
        }

        col.EnsureIndex(t => t.BackgroundKey);
    }
}
