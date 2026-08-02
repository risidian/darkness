using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Audit;

[Trait("Category", "Audit")]
public class EquipmentSpriteAudit
{
    private static JsonSerializerOptions BuildOptions()
    {
        var o = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        o.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        return o;
    }

    [Fact]
    public void EquipmentSpriteMappingIsValid()
    {
        var root = TestPaths.RepoRoot();
        var itemsPath = Path.Combine(root, "Darkness.Godot", "assets", "data", "items.json");
        var sheetDefsPath = Path.Combine(root, "Darkness.Godot", "assets", "data", "sheet_definitions");

        var itemsJson = File.ReadAllText(itemsPath);
        var items = JsonSerializer.Deserialize<List<Item>>(itemsJson, BuildOptions()) ?? new List<Item>();

        var sheetDefs = new List<SheetDefinition>();
        foreach (var file in Directory.GetFiles(sheetDefsPath, "*.json", SearchOption.AllDirectories))
        {
            var defJson = File.ReadAllText(file);
            var def = JsonSerializer.Deserialize<SheetDefinition>(defJson, BuildOptions());
            if (def != null)
                sheetDefs.Add(def);
        }

        var failures = new List<string>();
        var report = new List<string>();

        var referencedSheetDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var slotMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Body", "Armor" },
            { "Weapon", "Weapon" },
            { "Shield", "Shield" },
            { "Feet", "Feet" },
            { "Legs", "Legs" },
            { "Head", "Head" },
            { "Arms", "Arms" }
        };

        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.SpriteDefinitionName))
                continue;

            referencedSheetDefNames.Add(item.SpriteDefinitionName);

            var expectedSlot = item.EquipmentSlot ?? "None";
            if (slotMapping.TryGetValue(expectedSlot, out var mapped))
            {
                expectedSlot = mapped;
            }

            var matchingDef = sheetDefs.FirstOrDefault(d => 
                string.Equals(d.Name, item.SpriteDefinitionName, StringComparison.OrdinalIgnoreCase) && 
                string.Equals(d.Slot, expectedSlot, StringComparison.OrdinalIgnoreCase));

            if (matchingDef == null)
            {
                var nameOnlyMatch = sheetDefs.FirstOrDefault(d => string.Equals(d.Name, item.SpriteDefinitionName, StringComparison.OrdinalIgnoreCase));
                if (nameOnlyMatch != null)
                {
                    failures.Add($"Item '{item.Name}' uses SpriteDefinitionName '{item.SpriteDefinitionName}' with expected slot '{expectedSlot}', but the matching SheetDefinition has slot '{nameOnlyMatch.Slot}'.");
                }
                else
                {
                    failures.Add($"Item '{item.Name}' uses SpriteDefinitionName '{item.SpriteDefinitionName}', but no such SheetDefinition was found.");
                }
            }
        }

        var orphanedDefs = sheetDefs
            .Where(d => !referencedSheetDefNames.Contains(d.Name))
            .ToList();

        report.Add($"## Orphaned Items (Failures)");
        if (failures.Any())
        {
            foreach (var f in failures)
                report.Add($"- {f}");
        }
        else
        {
            report.Add("- None");
        }
        
        report.Add("");
        report.Add($"## Orphaned SheetDefinitions (Informational)");
        if (orphanedDefs.Any())
        {
            foreach (var def in orphanedDefs)
            {
                report.Add($"- {def.Name} (Slot: {def.Slot})");
            }
        }
        else
        {
            report.Add("- None");
        }

        var reportPath = Path.Combine(root, "TestResults", "equipment-sprite-audit.md");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, "# Equipment Sprite Audit\n\n" + string.Join("\n", report) + "\n");

        Assert.Empty(failures);
    }
}
