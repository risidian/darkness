using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Audit;

[Trait("Category", "Audit")]
public class LootDropAudit
{
    private static JsonSerializerOptions BuildOptions()
    {
        var o = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }

    [Fact]
    public void AllLootDropsReferenceValidItems()
    {
        var root = TestPaths.RepoRoot();
        var options = BuildOptions();

        // 1. Load all items
        var itemsPath = Path.Combine(root, "Darkness.Godot", "assets", "data", "items.json");
        var itemsJson = File.ReadAllText(itemsPath);
        var items = JsonSerializer.Deserialize<List<Item>>(itemsJson, options) ?? new List<Item>();
        var validItemNames = items.Select(i => i.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var failures = new List<string>();

        void CheckDrops(string context, IEnumerable<string> fixedDrops, IEnumerable<LootEntry> randomDrops)
        {
            if (fixedDrops != null)
            {
                foreach (var drop in fixedDrops)
                {
                    if (!validItemNames.Contains(drop))
                    {
                        failures.Add($"[{context}] FixedDrop references missing item '{drop}'");
                    }
                }
            }

            if (randomDrops != null)
            {
                foreach (var drop in randomDrops)
                {
                    if (!validItemNames.Contains(drop.ItemName))
                    {
                        failures.Add($"[{context}] RandomDrop references missing item '{drop.ItemName}'");
                    }
                }
            }
        }

        // 2. Load encounters
        var encountersPath = Path.Combine(root, "Darkness.Godot", "assets", "data", "encounters.json");
        if (File.Exists(encountersPath))
        {
            var encounters = JsonSerializer.Deserialize<List<EncounterTable>>(File.ReadAllText(encountersPath), options) ?? new List<EncounterTable>();
            foreach (var table in encounters)
            {
                foreach (var entry in table.Encounters)
                {
                    if (entry.Combat != null && entry.Combat.Enemies != null)
                    {
                        for (int i = 0; i < entry.Combat.Enemies.Count; i++)
                        {
                            var enemy = entry.Combat.Enemies[i];
                            string context = $"encounters.json -> BackgroundKey: {table.BackgroundKey} -> Enemy: {enemy.Name} (Index: {i})";
                            CheckDrops(context, enemy.FixedDrops, enemy.RandomDrops);
                        }
                    }
                }
            }
        }

        // 3. Load deathmatch encounters
        var deathmatchPath = Path.Combine(root, "Darkness.Godot", "assets", "data", "deathmatch-encounters.json");
        if (File.Exists(deathmatchPath))
        {
            var deathmatches = JsonSerializer.Deserialize<List<DeathmatchEncounter>>(File.ReadAllText(deathmatchPath), options) ?? new List<DeathmatchEncounter>();
            foreach (var dm in deathmatches)
            {
                if (dm.Enemies != null)
                {
                    for (int i = 0; i < dm.Enemies.Count; i++)
                    {
                        var enemy = dm.Enemies[i];
                        string context = $"deathmatch-encounters.json -> Encounter: {dm.Title} -> Enemy: {enemy.Name} (Index: {i})";
                        CheckDrops(context, enemy.FixedDrops, enemy.RandomDrops);
                    }
                }
            }
        }

        // 4. Load quests
        var chains = AuditContentLoader.LoadChains(root);
        foreach (var (file, chain) in chains)
        {
            if (chain.Steps != null)
            {
                foreach (var step in chain.Steps)
                {
                    if (step.Combat != null && step.Combat.Enemies != null)
                    {
                        for (int i = 0; i < step.Combat.Enemies.Count; i++)
                        {
                            var enemy = step.Combat.Enemies[i];
                            string context = $"quests/{file} -> Chain: {chain.Id} -> Step: {step.Id} -> Enemy: {enemy.Name} (Index: {i})";
                            CheckDrops(context, enemy.FixedDrops, enemy.RandomDrops);
                        }
                    }
                }
            }
        }

        // 5 & 6. Assert and list failures
        if (failures.Count > 0)
        {
            var message = "Orphaned loot drop references found:\n" + string.Join("\n", failures);
            Assert.Fail(message);
        }
    }
}
