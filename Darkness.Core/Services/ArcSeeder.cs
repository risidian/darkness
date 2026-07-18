using System;
using System.IO;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

public class ArcSeeder
{
    private readonly IFileSystemService _fileSystem;

    public ArcSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(ILiteDatabase db)
    {
        const string arcDir = "assets/data/arcs";

        if (!_fileSystem.DirectoryExists(arcDir))
        {
            // Behavior-neutral: no arcs directory yet means nothing to seed.
            Console.WriteLine($"[ArcSeeder] INFO: Arc directory not found, skipping: {arcDir}");
            return;
        }

        string[] files;
        try
        {
            files = _fileSystem.GetFiles(arcDir, "*.json");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ArcSeeder] ERROR: Failed to list arc files — {ex.Message}");
            return;
        }

        var arcCol = db.GetCollection<Arc>("arcs");
        var loadedIds = new List<string>();

        int arcCount = 0, errorCount = 0;

        foreach (var file in files)
        {
            try
            {
                var json = _fileSystem.ReadAllText(file);
                var arc = SystemJson.JsonSerializer.Deserialize<Arc>(json, new SystemJson.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });

                if (arc == null)
                {
                    Console.Error.WriteLine($"[ArcSeeder] WARN: File deserialized to null: {Path.GetFileName(file)}");
                    errorCount++;
                    continue;
                }

                arcCol.Upsert(arc);
                loadedIds.Add(arc.Id);
                arcCount++;
            }
            catch (SystemJson.JsonException ex)
            {
                Console.Error.WriteLine($"[ArcSeeder] ERROR: Failed to parse arc file: {Path.GetFileName(file)} — {ex.Message}");
                errorCount++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ArcSeeder] ERROR: Failed to read arc file: {Path.GetFileName(file)} — {ex.Message}");
                errorCount++;
            }
        }

        // Cleanup orphaned arcs
        arcCol.DeleteMany(x => !loadedIds.Contains(x.Id));

        arcCol.EnsureIndex(a => a.Id);

        Console.WriteLine($"[ArcSeeder] INFO: Synced {arcCount} arcs from {files.Length} files ({errorCount} errors)");
    }
}
