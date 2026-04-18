using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Darkness.Core.Models;
using Newtonsoft.Json;
using Xunit;

namespace Darkness.Tests.Audit;

public class QuestGraphAudit
{
    private const string QuestDataPath = "../../../Darkness.Godot/assets/data/quests/";

    [Fact]
    public void AuditAllQuests()
    {
        var root = FindSolutionRoot();
        var fullPath = Path.Combine(root, "Darkness.Godot", "assets", "data", "quests");
        
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Could not find quest data at {fullPath}");
        }

        var questFiles = Directory.GetFiles(fullPath, "*.json");
        var reports = new List<string>();

        foreach (var file in questFiles)
        {
            if (file.EndsWith(".bak")) continue;

            var content = File.ReadAllText(file);
            var chain = JsonConvert.DeserializeObject<QuestChain>(content);
            if (chain == null) continue;

            reports.Add($"## Audit for {chain.Id} ({Path.GetFileName(file)})");
            AuditChain(chain, reports);
        }

        var docPath = Path.Combine(root, "docs", "superpowers", "audit", "report-agent-3.md");
        var docDir = Path.GetDirectoryName(docPath);
        if (!string.IsNullOrEmpty(docDir) && !Directory.Exists(docDir))
        {
            Directory.CreateDirectory(docDir);
        }

        File.WriteAllText(docPath, 
            "# Quest Logic Audit Report\n\n" + string.Join("\n\n", reports));
    }

    private string FindSolutionRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current) && Directory.GetFiles(current, "Darkness.sln").Length == 0)
        {
            current = Path.GetDirectoryName(current);
        }
        return current ?? throw new Exception("Could not find Darkness.sln root");
    }

    private void AuditChain(QuestChain chain, List<string> reports)
    {
        var stepIds = chain.Steps.Select(s => s.Id).ToHashSet();
        
        foreach (var step in chain.Steps)
        {
            // Check for missing NextStepId
            if (step.Type != "branch" && !string.IsNullOrEmpty(step.NextStepId) && !stepIds.Contains(step.NextStepId))
            {
                reports.Add($"- [ERROR] Step `{step.Id}` (type: {step.Type}) points to non-existent `{step.NextStepId}`");
            }

            // Check if non-branch, non-terminal step has no NextStepId
            if (step.Type != "branch" && string.IsNullOrEmpty(step.NextStepId) && step != chain.Steps.Last())
            {
                // This might be okay if it's the intended end of a chain, but let's flag it if it's not the last step in the list
                reports.Add($"- [INFO] Step `{step.Id}` has no NextStepId and is not the last step in JSON.");
            }

            // Check Branch Options
            if (step.Type == "branch" && step.Branch != null)
            {
                if (!step.Branch.Options.Any())
                {
                    reports.Add($"- [ERROR] Branch step `{step.Id}` has no options.");
                }

                foreach (var opt in step.Branch.Options)
                {
                    if (string.IsNullOrEmpty(opt.NextStepId))
                    {
                        reports.Add($"- [ERROR] Step `{step.Id}` branch option `{opt.Text}` has empty NextStepId.");
                    }
                    else if (!stepIds.Contains(opt.NextStepId))
                    {
                        reports.Add($"- [ERROR] Step `{step.Id}` branch option `{opt.Text}` points to non-existent `{opt.NextStepId}`");
                    }

                    // Impossible condition check (basic)
                    if (opt.Conditions != null)
                    {
                        foreach (var cond in opt.Conditions)
                        {
                            if (cond.Type == "morality")
                            {
                                if (int.TryParse(cond.Value, out var val))
                                {
                                    if (val > 100 || val < -100) 
                                        reports.Add($"- [WARNING] Step `{step.Id}` has unlikely morality condition: {cond.Operator} {val}");
                                }
                            }
                            
                            // Check for invalid operators
                            var validOps = new[] { "==", "!=", ">=", "<=", ">", "<", "contains" };
                            if (!validOps.Contains(cond.Operator))
                            {
                                reports.Add($"- [ERROR] Step `{step.Id}` has invalid operator `{cond.Operator}` in condition.");
                            }
                        }
                    }
                }
                
                // Check if ALL options have conditions (potential soft-lock if none are met)
                if (step.Branch.Options.Count > 0 && step.Branch.Options.All(o => o.Conditions != null && o.Conditions.Any()))
                {
                    reports.Add($"- [INFO] Step `{step.Id}`: All options have conditions. Ensure at least one is always meetable.");
                }
            }

            // Combat specific check
            if (step.Type == "combat")
            {
                reports.Add($"- [CHECK] Combat step `{step.Id}`: Engine defaults to advancing on victory. No explicit failure path found in data.");
            }
        }
    }
}
