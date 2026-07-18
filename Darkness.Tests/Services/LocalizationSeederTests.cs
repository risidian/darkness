using Darkness.Core.Interfaces;
using Darkness.Core.Services;
using Moq;

namespace Darkness.Tests.Services;

public class LocalizationSeederTests
{
    private readonly Mock<IFileSystemService> _fsMock = new();

    [Fact]
    public void Seed_LoadsCatalogsIntoLocalizationService()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/i18n")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/i18n", "*.json", true))
            .Returns(new[] { "assets/data/i18n/en/quests.json" });
        _fsMock.Setup(f => f.ReadAllText("assets/data/i18n/en/quests.json"))
            .Returns("{ \"quest.beat_1.title\": \"The Awakening\" }");

        var loc = new LocalizationService();
        new LocalizationSeeder(loc, _fsMock.Object).Seed();

        Assert.Equal("The Awakening", loc.Resolve("quest.beat_1.title", "fallback"));
    }

    [Fact]
    public void Seed_MergesMultipleFilesForSameLocale()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/i18n")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/i18n", "*.json", true))
            .Returns(new[] { "assets/data/i18n/en/quests.json", "assets/data/i18n/en/arcs.json" });
        _fsMock.Setup(f => f.ReadAllText("assets/data/i18n/en/quests.json"))
            .Returns("{ \"quest.beat_1.title\": \"The Awakening\" }");
        _fsMock.Setup(f => f.ReadAllText("assets/data/i18n/en/arcs.json"))
            .Returns("{ \"arc.arc_1.title\": \"Darkness: Revenge\" }");

        var loc = new LocalizationService();
        new LocalizationSeeder(loc, _fsMock.Object).Seed();

        Assert.Equal("The Awakening", loc.Resolve("quest.beat_1.title"));
        Assert.Equal("Darkness: Revenge", loc.Resolve("arc.arc_1.title"));
    }

    [Fact]
    public void Seed_MissingDirectory_IsNoOp()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/i18n")).Returns(false);

        var loc = new LocalizationService();
        var ex = Record.Exception(() => new LocalizationSeeder(loc, _fsMock.Object).Seed());

        Assert.Null(ex);
        // Behavior-neutral: with no catalogs, Resolve returns the inline fallback.
        Assert.Equal("inline", loc.Resolve("quest.beat_1.title", "inline"));
    }

    [Fact]
    public void Seed_MalformedJson_SkipsFileAndContinues()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/i18n")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/i18n", "*.json", true))
            .Returns(new[] { "assets/data/i18n/en/bad.json", "assets/data/i18n/en/good.json" });
        _fsMock.Setup(f => f.ReadAllText("assets/data/i18n/en/bad.json")).Returns("{ not valid }");
        _fsMock.Setup(f => f.ReadAllText("assets/data/i18n/en/good.json"))
            .Returns("{ \"quest.beat_1.title\": \"The Awakening\" }");

        var loc = new LocalizationService();
        new LocalizationSeeder(loc, _fsMock.Object).Seed();

        Assert.Equal("The Awakening", loc.Resolve("quest.beat_1.title"));
    }
}
