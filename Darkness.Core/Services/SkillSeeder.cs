using System;
using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

public class SkillSeeder
{
    private readonly IFileSystemService _fileSystem;

    public SkillSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(ILiteDatabase db)
    {
        string json;
        try
        {
            json = _fileSystem.ReadAllText("assets/data/skills.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkillSeeder] ERROR: Failed to read skills.json — {ex.Message}");
            return;
        }

        List<Skill>? skills;
        try
        {
            skills = SystemJson.JsonSerializer.Deserialize<List<Skill>>(json, new SystemJson.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (SystemJson.JsonException ex)
        {
            Console.WriteLine($"[SkillSeeder] ERROR: Failed to parse skills.json — {ex.Message}");
            return;
        }

        if (skills == null || skills.Count == 0)
        {
            Console.WriteLine("[SkillSeeder] WARN: skills.json is empty or null");
            return;
        }

        var col = db.GetCollection<Skill>("skills");
        col.DeleteAll();
        foreach (var skill in skills)
            col.Insert(skill);
        col.EnsureIndex(s => s.Id);
        col.EnsureIndex(s => s.Name);

        Console.WriteLine($"[SkillSeeder] INFO: Loaded {col.Count()} skills from JSON");
    }
}
