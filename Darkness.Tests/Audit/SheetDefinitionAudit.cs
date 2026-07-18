using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Audit;

/// <summary>
/// Asserting audit over every equipment <see cref="SheetDefinition"/> JSON file.
///
/// Verifies two things for the on-disk source of truth the runtime
/// <c>SheetDefinitionSeeder</c> consumes:
///   1. Every JSON file deserializes cleanly (same settings as the seeder).
///   2. Every layer in every definition references sprite art that actually
///      exists on disk. A layer's declared path is a directory prefix that the
///      compositor expands into candidate PNG paths (see
///      <c>SkiaSharpSpriteCompositor.GetCandidatePaths</c>); this audit ports
///      that same candidate logic and requires each layer to resolve at least
///      one real PNG across its declared animations and both genders.
///
/// This makes a mistyped sprite path fail CI instead of silently rendering an
/// invisible / missing layer at runtime.
/// </summary>
[Trait("Category", "Audit")]
public class SheetDefinitionAudit
{
    private const string SpriteBase = "assets/sprites/full/";

    private static readonly JsonSerializerOptions Options =
        new() { PropertyNameCaseInsensitive = true };

    private static string DefinitionsDir(string repoRoot)
        => Path.Combine(repoRoot, "Darkness.Godot", "assets", "data", "sheet_definitions");

    private static string SpriteRoot(string repoRoot)
        => Path.Combine(repoRoot, "Darkness.Godot", "assets", "sprites", "full");

    [Fact]
    public void EverySheetDefinition_DeserializesCleanly()
    {
        var repoRoot = TestPaths.RepoRoot();
        var dir = DefinitionsDir(repoRoot);
        Assert.True(Directory.Exists(dir), $"Sheet definition directory missing: {dir}");

        var files = Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories);
        Assert.True(files.Length > 0, $"No sheet definition JSON files found under {dir}");

        var failures = new List<string>();
        foreach (var file in files)
        {
            SheetDefinition? def = null;
            try
            {
                def = JsonSerializer.Deserialize<SheetDefinition>(File.ReadAllText(file), Options);
            }
            catch (JsonException ex)
            {
                failures.Add($"{Rel(repoRoot, file)}: JSON error: {ex.Message}");
                continue;
            }

            if (def == null)
            {
                failures.Add($"{Rel(repoRoot, file)}: deserialized as null");
                continue;
            }
            if (string.IsNullOrWhiteSpace(def.Name))
                failures.Add($"{Rel(repoRoot, file)}: missing 'name'");
            if (string.IsNullOrWhiteSpace(def.Slot))
                failures.Add($"{Rel(repoRoot, file)}: missing 'slot'");
            if (def.Layers == null || def.Layers.Count == 0)
                failures.Add($"{Rel(repoRoot, file)}: has no layers");
        }

        Assert.True(failures.Count == 0,
            "Sheet definition deserialization failures:\n  " + string.Join("\n  ", failures));
    }

    [Fact]
    public void EverySheetDefinitionLayer_ResolvesAtLeastOneSpriteOnDisk()
    {
        var repoRoot = TestPaths.RepoRoot();
        var dir = DefinitionsDir(repoRoot);
        var spriteRoot = SpriteRoot(repoRoot);
        Assert.True(Directory.Exists(dir), $"Sheet definition directory missing: {dir}");
        Assert.True(Directory.Exists(spriteRoot), $"Sprite root missing: {spriteRoot}");

        var failures = new List<string>();

        foreach (var file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            var def = JsonSerializer.Deserialize<SheetDefinition>(File.ReadAllText(file), Options);
            if (def?.Layers == null) continue; // covered by the deserialization test

            // Animations that must be exercised for each layer. The compositor
            // always renders the standard animation rows, so fall back to a
            // representative default set if a definition declares none.
            var animations = (def.Animations != null && def.Animations.Count > 0)
                ? def.Animations
                : new List<string> { "walk", "idle", "slash", "thrust", "shoot", "hurt" };

            string variant = ResolveVariant(def);

            foreach (var (layerName, layer) in def.Layers)
            {
                bool resolved = false;
                var attempted = new List<string>();

                foreach (var gender in new[] { "male", "female" })
                {
                    string layerPath = layer.GetPath(gender);
                    if (string.IsNullOrEmpty(layerPath)) continue;

                    foreach (var animation in animations)
                    {
                        foreach (var candidate in GetCandidatePaths(layerPath, animation, variant))
                        {
                            attempted.Add(candidate);
                            if (File.Exists(Path.Combine(spriteRoot, candidate.Replace('/', Path.DirectorySeparatorChar))))
                            {
                                resolved = true;
                                break;
                            }
                        }
                        if (resolved) break;
                    }
                    if (resolved) break;
                }

                if (!resolved)
                {
                    var sample = attempted.Distinct().Take(6);
                    failures.Add(
                        $"{Rel(repoRoot, file)} :: layer '{layerName}' (variant '{variant}') resolved no sprite on disk. " +
                        $"Tried e.g.:\n      " + string.Join("\n      ", sample));
                }
            }
        }

        Assert.True(failures.Count == 0,
            "Sheet definition layers referencing missing sprite art:\n  " + string.Join("\n  ", failures));
    }

    // Port of SkiaSharpSpriteCompositor.ResolveVariant / ResolveLayers variant
    // selection: prefer an explicit non-"default" variant, else the first
    // declared variant.
    private static string ResolveVariant(SheetDefinition def)
    {
        var first = def.Variants != null && def.Variants.Count > 0 ? def.Variants[0] : null;
        if (!string.IsNullOrEmpty(first) && first != "default") return first;
        return "default";
    }

    // Faithful port of SkiaSharpSpriteCompositor.GetCandidatePaths. Returns the
    // sprite paths (relative to assets/sprites/full/) the compositor would try
    // for a given layer + animation + variant, plus the base-animation variants
    // for the oversize rows (the compositor renders those with the base name).
    private static IEnumerable<string> GetCandidatePaths(string layerPath, string animation, string variant)
    {
        var candidates = new List<string>();
        if (string.IsNullOrEmpty(layerPath)) return candidates;

        if (layerPath.StartsWith(SpriteBase, StringComparison.Ordinal))
            layerPath = layerPath.Substring(SpriteBase.Length);

        string basePath = layerPath.TrimEnd('/');

        void Emit(string anim)
        {
            candidates.Add($"{basePath}/{anim}/{variant}.png");
            if (variant != "default") candidates.Add($"{basePath}/{variant}.png");
            candidates.Add($"{basePath}/{variant}/{anim}.png");
            candidates.Add($"{basePath}/{anim}.png");

            if (anim == "slash" || anim == "thrust" || anim == "shoot")
            {
                candidates.Add($"{basePath}/attack_{anim}/{variant}.png");
                candidates.Add($"{basePath}/attack_{anim}.png");
            }
        }

        Emit(animation);

        // Oversize rows (e.g. "slash_oversize") are rendered by the compositor
        // using the base animation name; mirror that so custom oversize layers
        // resolve against their real art.
        if (animation.EndsWith("_oversize", StringComparison.Ordinal))
        {
            string baseAnim = animation.Replace("_oversize", "").Replace("_reverse", "");
            Emit(baseAnim);
        }

        if (layerPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            candidates.Add(layerPath);

        return candidates;
    }

    private static string Rel(string repoRoot, string full)
        => Path.GetRelativePath(repoRoot, full).Replace('\\', '/');
}
