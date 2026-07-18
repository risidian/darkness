using System;
using System.Collections.Generic;
using System.IO;
using Darkness.Core.Interfaces;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

/// <summary>
/// Loads i18n catalogs from disk into an <see cref="ILocalizationService"/>.
/// Reads <c>assets/data/i18n/{locale}/*.json</c>, each a flat <c>{ key: value }</c> map,
/// grouping files by their parent directory (the locale). Behavior-neutral: with no
/// <c>i18n</c> directory present, this is a no-op and the resolver keeps returning
/// inline-English fallbacks.
/// </summary>
public class LocalizationSeeder
{
    private readonly ILocalizationService _localization;
    private readonly IFileSystemService _fileSystem;

    public LocalizationSeeder(ILocalizationService localization, IFileSystemService fileSystem)
    {
        _localization = localization;
        _fileSystem = fileSystem;
    }

    public void Seed()
    {
        const string i18nDir = "assets/data/i18n";

        if (!_fileSystem.DirectoryExists(i18nDir))
        {
            // Behavior-neutral: no i18n directory yet means no catalogs to load.
            Console.WriteLine($"[LocalizationSeeder] INFO: i18n directory not found, skipping: {i18nDir}");
            return;
        }

        string[] files;
        try
        {
            // Recursive glob: locale is derived from each file's parent directory name.
            files = _fileSystem.GetFiles(i18nDir, "*.json", recursive: true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[LocalizationSeeder] ERROR: Failed to list i18n files — {ex.Message}");
            return;
        }

        var options = new SystemJson.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Accumulate entries per locale so multiple files for the same locale merge.
        var byLocale = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        int fileCount = 0, keyCount = 0, errorCount = 0;

        foreach (var file in files)
        {
            var locale = Path.GetFileName(Path.GetDirectoryName(file) ?? string.Empty);
            if (string.IsNullOrWhiteSpace(locale))
            {
                Console.Error.WriteLine($"[LocalizationSeeder] WARN: Could not derive locale for file: {file}");
                errorCount++;
                continue;
            }

            try
            {
                var json = _fileSystem.ReadAllText(file);
                var entries = SystemJson.JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);

                if (entries == null)
                {
                    Console.Error.WriteLine($"[LocalizationSeeder] WARN: File deserialized to null: {Path.GetFileName(file)}");
                    errorCount++;
                    continue;
                }

                if (!byLocale.TryGetValue(locale, out var acc))
                {
                    acc = new Dictionary<string, string>(StringComparer.Ordinal);
                    byLocale[locale] = acc;
                }

                foreach (var kvp in entries)
                {
                    acc[kvp.Key] = kvp.Value;
                    keyCount++;
                }

                fileCount++;
            }
            catch (SystemJson.JsonException ex)
            {
                Console.Error.WriteLine($"[LocalizationSeeder] ERROR: Failed to parse i18n file: {Path.GetFileName(file)} — {ex.Message}");
                errorCount++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[LocalizationSeeder] ERROR: Failed to read i18n file: {Path.GetFileName(file)} — {ex.Message}");
                errorCount++;
            }
        }

        foreach (var kvp in byLocale)
        {
            _localization.LoadCatalog(kvp.Key, kvp.Value);
        }

        Console.WriteLine($"[LocalizationSeeder] INFO: Loaded {keyCount} keys across {byLocale.Count} locale(s) from {fileCount} files ({errorCount} errors)");
    }
}
