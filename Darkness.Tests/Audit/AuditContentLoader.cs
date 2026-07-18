using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Darkness.Core.Models;

namespace Darkness.Tests.Audit;

/// <summary>
/// Shared loader for the JSON content graphs (quest chains + arcs) used by the
/// asserting content audits. Reads the same on-disk source of truth the
/// runtime seeders consume, using identical deserialization settings
/// (case-insensitive + string enum converter).
/// </summary>
internal static class AuditContentLoader
{
    private static readonly JsonSerializerOptions Options = BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        var o = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }

    public static string QuestDir(string repoRoot)
        => Path.Combine(repoRoot, "Darkness.Godot", "assets", "data", "quests");

    public static string ArcDir(string repoRoot)
        => Path.Combine(repoRoot, "Darkness.Godot", "assets", "data", "arcs");

    /// <summary>Loads every quest chain JSON file, keyed by source file name.</summary>
    public static List<(string File, QuestChain Chain)> LoadChains(string repoRoot)
    {
        var dir = QuestDir(repoRoot);
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Could not find quest data at {dir}");

        var result = new List<(string, QuestChain)>();
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            if (file.EndsWith(".bak")) continue;
            var chain = JsonSerializer.Deserialize<QuestChain>(File.ReadAllText(file), Options);
            if (chain != null)
                result.Add((Path.GetFileName(file), chain));
        }
        return result;
    }

    /// <summary>Loads every arc JSON file, keyed by source file name.</summary>
    public static List<(string File, Arc Arc)> LoadArcs(string repoRoot)
    {
        var dir = ArcDir(repoRoot);
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Could not find arc data at {dir}");

        var result = new List<(string, Arc)>();
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            if (file.EndsWith(".bak")) continue;
            var arc = JsonSerializer.Deserialize<Arc>(File.ReadAllText(file), Options);
            if (arc != null)
                result.Add((Path.GetFileName(file), arc));
        }
        return result;
    }
}
