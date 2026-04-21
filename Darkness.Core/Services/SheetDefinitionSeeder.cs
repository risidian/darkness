using System;
using System.Collections.Generic;
using System.IO;
using SystemJson = System.Text.Json;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class SheetDefinitionSeeder
{
    private readonly IFileSystemService _fileSystem;

    public SheetDefinitionSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(ILiteDatabase db)
    {
        var col = db.GetCollection<SheetDefinition>("sheet_definitions");
        var loadedIds = new List<int>();

        string baseDir = "assets/data/sheet_definitions";
        try
        {
            if (!_fileSystem.DirectoryExists(baseDir))
            {
                Console.WriteLine($"[SheetDefinitionSeeder] WARN: Directory not found: {baseDir}");
                return;
            }

            var files = _fileSystem.GetFiles(baseDir, "*.json", true);
            Console.WriteLine($"[SheetDefinitionSeeder] Found {files.Length} JSON files in {baseDir}");
            foreach (var file in files)
            {
                try
                {
                    string json = _fileSystem.ReadAllText(file);
                    var def = SystemJson.JsonSerializer.Deserialize<SheetDefinition>(json, new SystemJson.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (def != null)
                    {
                        col.Upsert(def);
                        loadedIds.Add(def.Id);
                        Console.WriteLine($"[SheetDefinitionSeeder] Synced: {def.Name} ({def.Slot}) from {file}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SheetDefinitionSeeder] ERROR: Failed to load {file}: {ex.Message}");
                }
            }

            // Cleanup orphaned definitions
            col.DeleteMany(x => !loadedIds.Contains(x.Id));

            col.EnsureIndex(x => x.Slot);
            col.EnsureIndex(x => x.Name);
            Console.WriteLine($"[SheetDefinitionSeeder] INFO: Synced {col.Count()} sheet definitions from {baseDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SheetDefinitionSeeder] ERROR: Seed failed: {ex.Message}");
        }
    }
}
