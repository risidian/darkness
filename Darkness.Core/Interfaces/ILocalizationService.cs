namespace Darkness.Core.Interfaces;

/// <summary>
/// Domain-owned localization lookup (Phase B: additive, behavior-neutral).
/// Catalogs are held in memory (locale -> key -> value); file-loading/DI wiring is deferred.
/// With no catalogs loaded, <see cref="Resolve"/> returns the supplied fallback (or the key
/// literal when no fallback is given), guaranteeing identical runtime behavior.
/// </summary>
public interface ILocalizationService
{
    /// <summary>The currently active locale (defaults to "en").</summary>
    string Locale { get; }

    /// <summary>
    /// Resolves a localization key against the active locale, then "en", then the supplied
    /// <paramref name="fallback"/>, then the key literal. Applies {token} interpolation to the
    /// resolved string using <paramref name="tokens"/>. Missing keys are logged once.
    /// </summary>
    string Resolve(string key, string? fallback = null, IReadOnlyDictionary<string, string>? tokens = null);

    /// <summary>Switches the active locale (restart-to-apply semantics for display sites).</summary>
    void SetLocale(string locale);

    /// <summary>Loads (merges) a parsed catalog for a locale into the in-memory store.</summary>
    void LoadCatalog(string locale, IReadOnlyDictionary<string, string> entries);
}
