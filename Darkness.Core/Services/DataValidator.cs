using System;
using System.Linq;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class DataValidator
{
    public static void Validate(ILiteDatabase db)
    {
        Console.WriteLine("[DataValidator] Running post-seed cross-validation...");
        int errorCount = 0;

        var items = db.GetCollection<Item>("items").FindAll().Select(i => i.Name).ToHashSet();
        var recipes = db.GetCollection<Recipe>("recipes").FindAll().ToList();

        // Validate Recipes
        foreach (var recipe in recipes)
        {
            foreach (var kvp in recipe.Materials)
            {
                if (!items.Contains(kvp.Key))
                {
                    Console.Error.WriteLine($"[DataValidator] ERROR: Recipe '{recipe.Name}' requires unknown material '{kvp.Key}'");
                    errorCount++;
                }
            }
        }

        // Validate Quests
        var questChains = db.GetCollection<QuestChain>("quest_chains").FindAll().ToList();
        foreach (var chain in questChains)
        {
            var stepIds = chain.Steps.Select(s => s.Id).ToHashSet();
            foreach (var step in chain.Steps)
            {
                if (step.Branch?.Options != null)
                {
                    foreach (var opt in step.Branch.Options)
                    {
                        if (!string.IsNullOrEmpty(opt.NextStepId) && !stepIds.Contains(opt.NextStepId))
                        {
                            Console.Error.WriteLine($"[DataValidator] ERROR: Quest Chain '{chain.Id}' Step '{step.Id}' branches to missing NextStepId '{opt.NextStepId}'");
                            errorCount++;
                        }
                    }
                }
            }
        }

        if (errorCount > 0)
        {
            throw new Exception($"Data validation failed with {errorCount} errors. See logs for details.");
        }
        Console.WriteLine("[DataValidator] Validation complete. All relationships intact.");
    }
}
