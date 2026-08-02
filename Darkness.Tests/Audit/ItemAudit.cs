using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Audit;

[Trait("Category", "Audit")]
public class ItemAudit
{
    private static readonly string[] ValidClasses = { "Knight", "Warrior", "Mage", "Rogue", "Cleric" };
    private static readonly string[] ValidSlots = { "Weapon", "Body", "Head", "Legs", "Feet", "Shield", "Arms" };

    [Fact]
    public void ItemDataIsValid()
    {
        var root = TestPaths.RepoRoot();
        var itemsPath = Path.Combine(root, "Darkness.Godot", "assets", "data", "items.json");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var json = File.ReadAllText(itemsPath);
        var items = JsonSerializer.Deserialize<List<Item>>(json, options);

        Assert.NotNull(items);

        var failures = new List<string>();
        var warnings = new List<string>();

        // 1. No duplicate IDs
        var dupIds = items.GroupBy(i => i.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        foreach (var id in dupIds) failures.Add($"Duplicate ID: {id}");

        // 2. No duplicate names
        var dupNames = items.GroupBy(i => i.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        foreach (var name in dupNames) failures.Add($"Duplicate Name: {name}");

        foreach (var item in items)
        {
            var ctx = $"Item {item.Id} ({item.Name})";

            // 6. Gold values are positive
            if (item.Value < 0) failures.Add($"{ctx}: Value cannot be negative ({item.Value}).");

            // 7. ClassRequirement is valid
            if (!string.IsNullOrEmpty(item.ClassRequirement) && !ValidClasses.Contains(item.ClassRequirement))
            {
                failures.Add($"{ctx}: Invalid ClassRequirement '{item.ClassRequirement}'.");
            }

            if (!string.IsNullOrEmpty(item.EquipmentSlot))
            {
                // 8. EquipmentSlot is valid
                if (!ValidSlots.Contains(item.EquipmentSlot))
                {
                    failures.Add($"{ctx}: Invalid EquipmentSlot '{item.EquipmentSlot}'.");
                }

                // 3. Equipment has stats
                bool hasStat = item.StrengthBonus != 0 || item.DexterityBonus != 0 || item.IntelligenceBonus != 0 || 
                               item.DefenseBonus != 0 || item.AttackBonus != 0 || item.ArmorClass != 0;
                
                if (!hasStat && item.Type != "Consumable")
                {
                    failures.Add($"{ctx}: Equipment must have at least one non-zero stat bonus or be of type Consumable.");
                }

                // 5. Equipment has SpriteDefinitionName (Warning)
                if (string.IsNullOrEmpty(item.SpriteDefinitionName))
                {
                    warnings.Add($"{ctx}: Missing SpriteDefinitionName (warning).");
                }
            }

            // 4. Consumables have effects
            if (item.Type == "Consumable" && item.Value <= 0)
            {
                failures.Add($"{ctx}: Consumable must have Value > 0.");
            }
        }

        var reportPath = Path.Combine(root, "TestResults", "item-audit.md");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        var reportContent = "# Item Audit\n\n";
        if (failures.Count == 0 && warnings.Count == 0) reportContent += "- OK\n";
        else 
        {
            if (failures.Count > 0) reportContent += "## Failures\n" + string.Join("\n", failures.Select(f => $"- {f}")) + "\n\n";
            if (warnings.Count > 0) reportContent += "## Warnings\n" + string.Join("\n", warnings.Select(w => $"- {w}")) + "\n\n";
        }
        File.WriteAllText(reportPath, reportContent);

        Assert.Empty(failures);
    }
}
