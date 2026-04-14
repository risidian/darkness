using System;
using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

public class RecipeSeeder
{
    private readonly IFileSystemService _fs;

    public RecipeSeeder(IFileSystemService fs) { _fs = fs; }

    public void Seed(LiteDatabase db)
    {
        string json;
        try
        {
            json = _fs.ReadAllText("assets/data/recipes.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RecipeSeeder] ERROR: Failed to read recipes.json — {ex.Message}");
            return;
        }

        List<Recipe>? recipes;
        try
        {
            recipes = SystemJson.JsonSerializer.Deserialize<List<Recipe>>(json,
                new SystemJson.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (SystemJson.JsonException ex)
        {
            Console.WriteLine($"[RecipeSeeder] ERROR: Failed to parse recipes.json — {ex.Message}");
            return;
        }

        if (recipes == null || recipes.Count == 0)
        {
            Console.WriteLine("[RecipeSeeder] WARN: recipes.json is empty or null");
            return;
        }

        var col = db.GetCollection<Recipe>("recipes");
        col.DeleteAll();
        col.InsertBulk(recipes);
        col.EnsureIndex(r => r.Name);

        Console.WriteLine($"[RecipeSeeder] INFO: Loaded {col.Count()} recipes from JSON");
    }
}
