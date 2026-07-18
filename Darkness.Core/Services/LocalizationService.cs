using System;
using System.Collections.Generic;
using System.Text;
using Darkness.Core.Interfaces;

namespace Darkness.Core.Services;

/// <summary>
/// In-memory <see cref="ILocalizationService"/>. Backing store is locale -> key -> value.
/// Behavior-neutral by design: with no catalogs loaded, Resolve returns the inline-English
/// fallback (or the key literal when none), so display sites render identically to today.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private const string DefaultLocale = "en";

    // locale -> (key -> value)
    private readonly Dictionary<string, Dictionary<string, string>> _catalogs = new(StringComparer.OrdinalIgnoreCase);

    // Keys already reported missing, so we log each at most once.
    private readonly HashSet<string> _reportedMissing = new(StringComparer.Ordinal);

    public string Locale { get; private set; } = DefaultLocale;

    public void SetLocale(string locale)
    {
        Locale = string.IsNullOrWhiteSpace(locale) ? DefaultLocale : locale;
    }

    public void LoadCatalog(string locale, IReadOnlyDictionary<string, string> entries)
    {
        if (string.IsNullOrWhiteSpace(locale)) throw new ArgumentException("Locale must be provided.", nameof(locale));
        if (entries == null) throw new ArgumentNullException(nameof(entries));

        if (!_catalogs.TryGetValue(locale, out var catalog))
        {
            catalog = new Dictionary<string, string>(StringComparer.Ordinal);
            _catalogs[locale] = catalog;
        }

        foreach (var kvp in entries)
        {
            catalog[kvp.Key] = kvp.Value;
        }
    }

    public string Resolve(string key, string? fallback = null, IReadOnlyDictionary<string, string>? tokens = null)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        string? value = null;

        // 1. active locale
        if (!TryLookup(Locale, key, out value))
        {
            // 2. default locale ("en")
            if (!string.Equals(Locale, DefaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                TryLookup(DefaultLocale, key, out value);
            }
        }

        if (value == null)
        {
            // Missing from all catalogs — log once, then fall back to inline text, then key literal.
            if (_reportedMissing.Add(key))
            {
                Console.Error.WriteLine($"[LocalizationService] WARN: Missing localization key '{key}' (locale '{Locale}')");
            }
            value = fallback ?? key;
        }

        return Interpolate(value, tokens);
    }

    private bool TryLookup(string locale, string key, out string? value)
    {
        value = null;
        return _catalogs.TryGetValue(locale, out var catalog) && catalog.TryGetValue(key, out value);
    }

    /// <summary>
    /// Replaces {token} placeholders with matching values. Unmatched placeholders are left
    /// verbatim (QA sentinel). No-op when there are no tokens or no braces.
    /// </summary>
    private static string Interpolate(string text, IReadOnlyDictionary<string, string>? tokens)
    {
        if (tokens == null || tokens.Count == 0 || text.IndexOf('{') < 0)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (c == '{')
            {
                int close = text.IndexOf('}', i + 1);
                if (close > i)
                {
                    var name = text.Substring(i + 1, close - i - 1);
                    if (tokens.TryGetValue(name, out var replacement))
                    {
                        sb.Append(replacement);
                        i = close + 1;
                        continue;
                    }
                }
            }
            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }
}
