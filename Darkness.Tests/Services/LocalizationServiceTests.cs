using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Services;
using Xunit;

namespace Darkness.Tests.Services
{
    public class LocalizationServiceTests
    {
        private static ILocalizationService NewService() => new LocalizationService();

        [Fact]
        public void DefaultLocale_IsEnglish()
        {
            Assert.Equal("en", NewService().Locale);
        }

        [Fact]
        public void EmptyStore_ReturnsFallback()
        {
            var loc = NewService();
            Assert.Equal("Inline English", loc.Resolve("quest.beat_1.title", "Inline English"));
        }

        [Fact]
        public void EmptyStore_NoFallback_ReturnsKeyLiteral()
        {
            var loc = NewService();
            Assert.Equal("quest.beat_1.title", loc.Resolve("quest.beat_1.title"));
        }

        [Fact]
        public void ActiveLocaleHit_WinsOverEnAndFallback()
        {
            var loc = NewService();
            loc.LoadCatalog("en", new Dictionary<string, string> { ["greeting"] = "Hello" });
            loc.LoadCatalog("fr", new Dictionary<string, string> { ["greeting"] = "Bonjour" });
            loc.SetLocale("fr");

            Assert.Equal("Bonjour", loc.Resolve("greeting", "fallback"));
        }

        [Fact]
        public void FallsBackToEn_WhenMissingInActiveLocale()
        {
            var loc = NewService();
            loc.LoadCatalog("en", new Dictionary<string, string> { ["greeting"] = "Hello" });
            loc.SetLocale("fr"); // no fr catalog for this key

            Assert.Equal("Hello", loc.Resolve("greeting", "fallback"));
        }

        [Fact]
        public void FallsBackToProvidedFallback_WhenMissingEverywhere()
        {
            var loc = NewService();
            loc.LoadCatalog("en", new Dictionary<string, string> { ["other"] = "Value" });

            Assert.Equal("fallback", loc.Resolve("greeting", "fallback"));
        }

        [Fact]
        public void FallsBackToKeyLiteral_WhenNoFallbackAndMissing()
        {
            var loc = NewService();
            loc.LoadCatalog("en", new Dictionary<string, string> { ["other"] = "Value" });

            Assert.Equal("greeting", loc.Resolve("greeting"));
        }

        [Fact]
        public void SetLocale_SwitchesActiveLocale()
        {
            var loc = NewService();
            loc.LoadCatalog("en", new Dictionary<string, string> { ["k"] = "English" });
            loc.LoadCatalog("de", new Dictionary<string, string> { ["k"] = "Deutsch" });

            Assert.Equal("English", loc.Resolve("k"));
            loc.SetLocale("de");
            Assert.Equal("de", loc.Locale);
            Assert.Equal("Deutsch", loc.Resolve("k"));
        }

        [Fact]
        public void Interpolation_ReplacesSingleToken()
        {
            var loc = NewService();
            loc.LoadCatalog("en", new Dictionary<string, string> { ["welcome"] = "Welcome, {playerName}!" });

            var result = loc.Resolve("welcome", null, new Dictionary<string, string> { ["playerName"] = "Arthur" });
            Assert.Equal("Welcome, Arthur!", result);
        }

        [Fact]
        public void Interpolation_ReplacesMultipleTokens()
        {
            var loc = NewService();
            loc.LoadCatalog("en", new Dictionary<string, string> { ["msg"] = "{who} strikes {target} for {amount}!" });

            var result = loc.Resolve("msg", null, new Dictionary<string, string>
            {
                ["who"] = "Merlin",
                ["target"] = "Goblin",
                ["amount"] = "12"
            });
            Assert.Equal("Merlin strikes Goblin for 12!", result);
        }

        [Fact]
        public void Interpolation_AppliesToFallbackString()
        {
            var loc = NewService();
            // key not loaded; fallback carries the token
            var result = loc.Resolve("welcome", "Hi {playerName}", new Dictionary<string, string> { ["playerName"] = "Guin" });
            Assert.Equal("Hi Guin", result);
        }

        [Fact]
        public void Interpolation_LeavesUnknownTokenVerbatim()
        {
            var loc = NewService();
            loc.LoadCatalog("en", new Dictionary<string, string> { ["welcome"] = "Welcome, {playerName}!" });

            var result = loc.Resolve("welcome", null, new Dictionary<string, string> { ["other"] = "x" });
            Assert.Equal("Welcome, {playerName}!", result);
        }

        [Fact]
        public void Interpolation_NoTokens_ReturnsResolvedStringUnchanged()
        {
            var loc = NewService();
            loc.LoadCatalog("en", new Dictionary<string, string> { ["welcome"] = "Welcome, {playerName}!" });

            // No token dictionary provided -> placeholder left as-is.
            Assert.Equal("Welcome, {playerName}!", loc.Resolve("welcome"));
        }

        [Fact]
        public void LoadCatalog_MergesEntriesForSameLocale()
        {
            var loc = NewService();
            loc.LoadCatalog("en", new Dictionary<string, string> { ["a"] = "A1" });
            loc.LoadCatalog("en", new Dictionary<string, string> { ["b"] = "B1", ["a"] = "A2" });

            Assert.Equal("A2", loc.Resolve("a"));
            Assert.Equal("B1", loc.Resolve("b"));
        }
    }
}
