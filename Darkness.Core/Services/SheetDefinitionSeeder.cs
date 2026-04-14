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
        col.DeleteAll();

        string baseDir = "assets/data/sheet_definitions";
        try
        {
            if (!_fileSystem.DirectoryExists(baseDir))
            {
                Console.WriteLine($"[SheetDefinitionSeeder] WARN: Directory not found: {baseDir}");
                return;
            }

            var files = _fileSystem.GetFiles(baseDir, "*.json", true);
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
                        col.Insert(def);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SheetDefinitionSeeder] ERROR: Failed to load {file}: {ex.Message}");
                }
            }

            col.EnsureIndex(x => x.Slot);
            col.EnsureIndex(x => x.Name);
            Console.WriteLine($"[SheetDefinitionSeeder] INFO: Loaded {col.Count()} sheet definitions from {baseDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SheetDefinitionSeeder] ERROR: Seed failed: {ex.Message}");
        }
    }
}
