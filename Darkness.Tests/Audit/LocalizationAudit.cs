using System.Collections.Generic;
using System.IO;
using System.Linq;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Audit;

[Trait("Category", "Audit")]
public class LocalizationAudit
{
    [Fact]
    public void ExtractionProducesValidCatalog()
    {
        var root = TestPaths.RepoRoot();
        var chains = AuditContentLoader.LoadChains(root);
        
        var keys = new HashSet<string>();
        var duplicates = new List<string>();
        var emptyValues = new List<string>();

        foreach (var (file, chain) in chains)
        {
            var chainId = chain.Id;
            if (string.IsNullOrEmpty(chainId)) continue;

            void AddKey(string key, string value)
            {
                if (!keys.Add(key))
                {
                    duplicates.Add(key);
                }
                if (string.IsNullOrWhiteSpace(value))
                {
                    emptyValues.Add(key);
                }
            }

            if (!string.IsNullOrEmpty(chain.Title)) AddKey($"{chainId}.title", chain.Title);
            
            // Note: Description isn't on QuestChain model by default, but let's assume if it exists we'd get it.
            // Wait, we don't need to check fields that aren't loaded in C# model if we just check what's loaded.
            
            foreach (var step in chain.Steps)
            {
                var stepId = step.Id;
                if (string.IsNullOrEmpty(stepId)) continue;

                if (step.Dialogue != null)
                {
                    if (!string.IsNullOrEmpty(step.Dialogue.Speaker))
                        AddKey($"{chainId}.{stepId}.speaker", step.Dialogue.Speaker);
                    
                    for (int i = 0; i < step.Dialogue.Lines.Count; i++)
                    {
                        var line = step.Dialogue.Lines[i];
                        if (!string.IsNullOrEmpty(line))
                            AddKey($"{chainId}.{stepId}.line_{i}", line);
                    }
                }

                if (step.Branch != null)
                {
                    for (int i = 0; i < step.Branch.Options.Count; i++)
                    {
                        var opt = step.Branch.Options[i];
                        if (!string.IsNullOrEmpty(opt.Text))
                            AddKey($"{chainId}.{stepId}.option_{i}.text", opt.Text);
                    }
                }

                if (step.Combat != null)
                {
                    for (int i = 0; i < step.Combat.Enemies.Count; i++)
                    {
                        var enemy = step.Combat.Enemies[i];
                        if (!string.IsNullOrEmpty(enemy.Name))
                            AddKey($"{chainId}.{stepId}.enemy_{i}.name", enemy.Name);
                    }
                    for (int i = 0; i < step.Combat.Allies.Count; i++)
                    {
                        var ally = step.Combat.Allies[i];
                        if (!string.IsNullOrEmpty(ally.Name))
                            AddKey($"{chainId}.{stepId}.ally_{i}.name", ally.Name);
                    }
                }

                if (step.Visuals?.Npc != null && !string.IsNullOrEmpty(step.Visuals.Npc.Name))
                {
                    AddKey($"{chainId}.{stepId}.npc.name", step.Visuals.Npc.Name);
                }
            }
        }

        Assert.Empty(duplicates);
        Assert.Empty(emptyValues);
        Assert.NotEmpty(keys); // Make sure we found some!
    }
}
