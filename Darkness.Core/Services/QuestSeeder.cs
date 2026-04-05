using System;
using System.IO;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

public class QuestSeeder
{
    private readonly IFileSystemService _fileSystem;

    public QuestSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(LiteDatabase db)
    {
        const string questDir = "assets/data/quests";

        if (!_fileSystem.DirectoryExists(questDir))
        {
            Console.WriteLine($"[QuestSeeder] ERROR: Quest directory not found: {questDir}");
            return;
        }

        string[] files;
        try
        {
            files = _fileSystem.GetFiles(questDir, "*.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuestSeeder] ERROR: Failed to list quest files — {ex.Message}");
            return;
        }

        var chainCol = db.GetCollection<QuestChain>("quest_chains");
        chainCol.DeleteAll();

        int chainCount = 0, stepCount = 0, errorCount = 0;

        foreach (var file in files)
        {
            try
            {
                var json = _fileSystem.ReadAllText(file);
                var chain = SystemJson.JsonSerializer.Deserialize<QuestChain>(json, new SystemJson.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (chain == null)
                {
                    Console.WriteLine($"[QuestSeeder] WARN: File deserialized to null: {Path.GetFileName(file)}");
                    errorCount++;
                    continue;
                }

                chainCol.Upsert(chain);
                chainCount++;
                stepCount += chain.Steps.Count;
            }
            catch (SystemJson.JsonException ex)
            {
                Console.WriteLine($"[QuestSeeder] ERROR: Failed to parse quest file: {Path.GetFileName(file)} — {ex.Message}");
                errorCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QuestSeeder] ERROR: Failed to read quest file: {Path.GetFileName(file)} — {ex.Message}");
                errorCount++;
            }
        }

        chainCol.EnsureIndex(c => c.Id);
        chainCol.EnsureIndex(c => c.IsMainStory);

        Console.WriteLine($"[QuestSeeder] INFO: Loaded {chainCount} quest chains with {stepCount} steps from {files.Length} files ({errorCount} errors)");
    }
}
