using System.Collections.Generic;
using System.IO;
using System.Linq;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Audit;

/// <summary>
/// Asserting audit of the arc layer that sits atop the quest graph. Verifies
/// arc<->chain referential consistency (bidirectional), that arc and chain
/// prerequisite lists form DAGs (no cycles, all ids resolve), unique arc
/// ordering, unique SortOrder within each arc's main line, and that every
/// main-story chain is reachable through some arc's main chain list.
/// </summary>
[Trait("Category", "Audit")]
public class ArcGraphAudit
{
    [Fact]
    public void ArcGraphIsValid()
    {
        var root = TestPaths.RepoRoot();
        var arcs = AuditContentLoader.LoadArcs(root).Select(t => t.Arc).ToList();
        var chains = AuditContentLoader.LoadChains(root).Select(t => t.Chain).ToList();

        var failures = new List<string>();

        var arcById = arcs.ToDictionary(a => a.Id);
        var chainById = chains.ToDictionary(c => c.Id);

        // --- 1. Arc -> chain references resolve to loaded chains ---
        foreach (var arc in arcs)
        {
            foreach (var id in arc.MainChainIds.Concat(arc.SideChainIds))
            {
                if (!chainById.ContainsKey(id))
                    failures.Add($"Arc `{arc.Id}` references non-existent chain `{id}`.");
            }
        }

        // --- 1b. Bidirectional consistency: a chain's ArcId must be listed by that arc ---
        foreach (var chain in chains)
        {
            if (string.IsNullOrEmpty(chain.ArcId)) continue;

            if (!arcById.TryGetValue(chain.ArcId, out var arc))
            {
                failures.Add($"Chain `{chain.Id}` has ArcId `{chain.ArcId}` but no such arc exists.");
                continue;
            }

            if (!arc.MainChainIds.Contains(chain.Id) && !arc.SideChainIds.Contains(chain.Id))
                failures.Add($"Chain `{chain.Id}` claims ArcId `{chain.ArcId}` but that arc does not list it.");
        }

        // A chain listed by an arc should also declare that ArcId (reverse direction).
        foreach (var arc in arcs)
        {
            foreach (var id in arc.MainChainIds.Concat(arc.SideChainIds))
            {
                if (chainById.TryGetValue(id, out var chain) && chain.ArcId != arc.Id)
                    failures.Add($"Arc `{arc.Id}` lists chain `{id}` but that chain's ArcId is `{chain.ArcId ?? "null"}`.");
            }
        }

        // --- 2. Prerequisite DAGs (arcs and chains) ---
        CheckDag(
            "Arc",
            arcs.ToDictionary(a => a.Id, a => (IReadOnlyList<string>)a.Prerequisites),
            failures);
        CheckDag(
            "Chain",
            chains.ToDictionary(c => c.Id, c => (IReadOnlyList<string>)c.Prerequisites),
            failures);

        // --- 3. Unique Arc.Order ---
        foreach (var g in arcs.GroupBy(a => a.Order).Where(g => g.Count() > 1))
            failures.Add($"Arc.Order {g.Key} is shared by arcs: {string.Join(", ", g.Select(a => a.Id))}.");

        // --- 3b. Unique SortOrder within each arc's main line ---
        foreach (var arc in arcs)
        {
            var mainChains = arc.MainChainIds
                .Where(chainById.ContainsKey)
                .Select(id => chainById[id])
                .ToList();

            foreach (var g in mainChains.GroupBy(c => c.SortOrder).Where(g => g.Count() > 1))
                failures.Add($"Arc `{arc.Id}` main line has duplicate SortOrder {g.Key}: {string.Join(", ", g.Select(c => c.Id))}.");
        }

        // --- 4. Every IsMainStory chain is reachable via some arc's MainChainIds ---
        var mainLineIds = arcs.SelectMany(a => a.MainChainIds).ToHashSet();
        foreach (var chain in chains.Where(c => c.IsMainStory))
        {
            if (!mainLineIds.Contains(chain.Id))
                failures.Add($"Main-story chain `{chain.Id}` is not listed in any arc's MainChainIds.");
        }

        Assert.Empty(failures);
    }

    /// <summary>
    /// Verifies a prerequisite graph is a DAG: every referenced id exists, and
    /// there are no cycles (detected via DFS three-colour marking).
    /// </summary>
    private static void CheckDag(string label, Dictionary<string, IReadOnlyList<string>> deps, List<string> failures)
    {
        foreach (var (id, prereqs) in deps)
        {
            foreach (var p in prereqs)
            {
                if (!deps.ContainsKey(p))
                    failures.Add($"{label} `{id}` has non-existent prerequisite `{p}`.");
            }
        }

        // 0 = unvisited, 1 = in-progress, 2 = done
        var state = deps.Keys.ToDictionary(k => k, _ => 0);
        var cyclesReported = new HashSet<string>();

        void Visit(string node, List<string> stack)
        {
            if (!state.ContainsKey(node)) return; // dangling prereq already reported
            if (state[node] == 2) return;
            if (state[node] == 1)
            {
                var cycleStart = stack.IndexOf(node);
                var cycle = stack.Skip(cycleStart).Append(node);
                var key = string.Join("->", cycle);
                if (cyclesReported.Add(key))
                    failures.Add($"{label} prerequisite cycle detected: {key}.");
                return;
            }

            state[node] = 1;
            stack.Add(node);
            foreach (var p in deps[node])
                Visit(p, stack);
            stack.RemoveAt(stack.Count - 1);
            state[node] = 2;
        }

        foreach (var id in deps.Keys)
            Visit(id, new List<string>());
    }
}
