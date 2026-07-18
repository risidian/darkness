using System.Collections.Generic;
using System.IO;
using System.Linq;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Audit;

/// <summary>
/// Asserting audit of the quest content graph. Loads the on-disk quest JSON
/// (the same source the runtime seeder consumes) and fails the build if any
/// chain is structurally broken: dangling step targets, empty/invalid
/// branches, unreachable steps, or a soft-locked chain with no terminal path.
/// </summary>
[Trait("Category", "Audit")]
public class QuestGraphAudit
{
    private static readonly string[] ValidOperators =
        { "==", "!=", ">=", "<=", ">", "<", "contains" };

    [Fact]
    public void QuestGraphIsValid()
    {
        var root = TestPaths.RepoRoot();
        var chains = AuditContentLoader.LoadChains(root);

        var failures = new List<string>();
        var report = new List<string>();

        foreach (var (file, chain) in chains)
        {
            report.Add($"## {chain.Id} ({file})");
            AuditChain(chain, file, failures, report);
        }

        WriteReport(root, report);

        Assert.Empty(failures);
    }

    private void AuditChain(QuestChain chain, string file, List<string> failures, List<string> report)
    {
        string Ctx(string stepId) => $"[{file}:{chain.Id}] step `{stepId}`";

        if (chain.Steps.Count == 0)
        {
            failures.Add($"[{file}:{chain.Id}] chain has no steps.");
            return;
        }

        // Duplicate step ids break target resolution and reachability.
        var duplicateIds = chain.Steps
            .GroupBy(s => s.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        foreach (var dup in duplicateIds)
            failures.Add($"[{file}:{chain.Id}] duplicate step id `{dup}`.");

        var stepIds = chain.Steps.Select(s => s.Id).ToHashSet();

        foreach (var step in chain.Steps)
        {
            // A branch-typed step must carry options.
            if (step.Type == QuestStepType.Branch && (step.Branch == null || step.Branch.Options.Count == 0))
                failures.Add($"{Ctx(step.Id)} is a branch with no options.");

            // NextStepId is valid on any step type and must resolve when present.
            if (!string.IsNullOrEmpty(step.NextStepId) && !stepIds.Contains(step.NextStepId))
                failures.Add($"{Ctx(step.Id)} (type {step.Type}) targets non-existent step `{step.NextStepId}`.");

            // Branch options may appear on any step type (e.g. Travel hubs) and are validated wherever present.
            if (step.Branch != null)
            {
                foreach (var opt in step.Branch.Options)
                {
                    if (string.IsNullOrEmpty(opt.NextStepId))
                        failures.Add($"{Ctx(step.Id)} branch option \"{opt.Text}\" has an empty NextStepId.");
                    else if (!stepIds.Contains(opt.NextStepId))
                        failures.Add($"{Ctx(step.Id)} branch option \"{opt.Text}\" targets non-existent step `{opt.NextStepId}`.");

                    ValidateConditions(opt.Conditions, $"{Ctx(step.Id)} branch option \"{opt.Text}\"", failures);
                }
            }

            ValidateConditions(step.Requirements, $"{Ctx(step.Id)} requirement", failures);
        }

        // Reachability + terminal-path analysis from the entry step (Steps[0]).
        var byId = chain.Steps.ToDictionary(s => s.Id);
        var reachable = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(chain.Steps[0].Id);
        reachable.Add(chain.Steps[0].Id);

        bool reachableTerminalExists = false;

        while (queue.Count > 0)
        {
            var current = byId[queue.Dequeue()];
            var outgoing = OutgoingTargets(current, stepIds).ToList();

            if (outgoing.Count == 0)
                reachableTerminalExists = true;

            foreach (var target in outgoing)
            {
                if (reachable.Add(target))
                    queue.Enqueue(target);
            }
        }

        var unreachable = chain.Steps.Select(s => s.Id).Where(id => !reachable.Contains(id)).ToList();
        foreach (var id in unreachable)
            failures.Add($"[{file}:{chain.Id}] step `{id}` is unreachable from entry step `{chain.Steps[0].Id}`.");

        if (!reachableTerminalExists)
            failures.Add($"[{file}:{chain.Id}] has no reachable terminal step (potential soft-lock).");

        report.Add(unreachable.Count == 0 && !duplicateIds.Any()
            ? "- OK"
            : $"- {unreachable.Count} unreachable, {duplicateIds.Count} duplicate id(s)");
    }

    /// <summary>
    /// Resolves the set of valid, existing step ids a step can transition to:
    /// the union of NextStepId and any branch-option targets. Branch blocks can
    /// appear on any step type (e.g. Travel hubs in exploration_demo), so both
    /// edge sources are considered regardless of Type. Empty or dangling targets
    /// are excluded here (reported as failures elsewhere).
    /// </summary>
    private static IEnumerable<string> OutgoingTargets(QuestStep step, HashSet<string> stepIds)
    {
        if (!string.IsNullOrEmpty(step.NextStepId) && stepIds.Contains(step.NextStepId))
            yield return step.NextStepId;

        if (step.Branch != null)
        {
            foreach (var opt in step.Branch.Options)
            {
                if (!string.IsNullOrEmpty(opt.NextStepId) && stepIds.Contains(opt.NextStepId))
                    yield return opt.NextStepId;
            }
        }
    }

    private void ValidateConditions(IEnumerable<BranchCondition>? conditions, string context, List<string> failures)
    {
        if (conditions == null) return;
        foreach (var cond in conditions)
        {
            if (!ValidOperators.Contains(cond.Operator))
                failures.Add($"{context} has invalid branch-condition operator `{cond.Operator}`.");
        }
    }

    private static void WriteReport(string root, List<string> report)
    {
        var reportPath = Path.Combine(root, "TestResults", "quest-graph-audit.md");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, "# Quest Graph Audit\n\n" + string.Join("\n", report) + "\n");
    }
}
