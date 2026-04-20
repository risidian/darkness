using System;
using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

public class RewardSeeder
{
    private readonly IFileSystemService _fileSystem;

    public RewardSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(ILiteDatabase db)
    {
        SeedRandomRewards(db);
        SeedCalendarRewards(db);
    }

    private void SeedRandomRewards(ILiteDatabase db)
    {
        string json;
        try
        {
            json = _fileSystem.ReadAllText("assets/data/random-rewards.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RewardSeeder] ERROR: Failed to read random-rewards.json — {ex.Message}");
            return;
        }

        List<RandomReward>? rewards;
        try
        {
            rewards = SystemJson.JsonSerializer.Deserialize<List<RandomReward>>(json, new SystemJson.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (SystemJson.JsonException ex)
        {
            Console.WriteLine($"[RewardSeeder] ERROR: Failed to parse random-rewards.json — {ex.Message}");
            return;
        }

        if (rewards != null)
        {
            var col = db.GetCollection<RandomReward>("random_rewards");
            col.DeleteAll();
            foreach (var r in rewards)
                col.Insert(r);
            Console.WriteLine($"[RewardSeeder] INFO: Loaded {col.Count()} random rewards");
        }
    }

    private void SeedCalendarRewards(ILiteDatabase db)
    {
        string json;
        try
        {
            json = _fileSystem.ReadAllText("assets/data/login-calendar.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RewardSeeder] ERROR: Failed to read login-calendar.json — {ex.Message}");
            return;
        }

        List<CalendarReward>? rewards;
        try
        {
            rewards = SystemJson.JsonSerializer.Deserialize<List<CalendarReward>>(json, new SystemJson.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (SystemJson.JsonException ex)
        {
            Console.WriteLine($"[RewardSeeder] ERROR: Failed to parse login-calendar.json — {ex.Message}");
            return;
        }

        if (rewards != null)
        {
            var col = db.GetCollection<CalendarReward>("login_calendar");
            col.DeleteAll();
            foreach (var r in rewards)
                col.Insert(r);
            col.EnsureIndex(r => r.Month);
            Console.WriteLine($"[RewardSeeder] INFO: Loaded {col.Count()} calendar reward sets");
        }
    }
}
