using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Moq;
using SystemJson = System.Text.Json;

namespace Darkness.Tests.Services;

public class ArcSeederTests : IDisposable
{
    private readonly Mock<IFileSystemService> _fsMock;
    private readonly string _dbPath;
    private readonly LiteDatabase _db;

    public ArcSeederTests()
    {
        _fsMock = new Mock<IFileSystemService>();
        _dbPath = Path.Combine(Path.GetTempPath(), $"ArcSeederTests_{Guid.NewGuid()}.db");
        // Each LiteDatabase gets its OWN BsonMapper (flaky-test fix — mandatory).
        _db = new LiteDatabase(_dbPath, new BsonMapper());
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    private static string MakeArcJson(string id, int order, params string[] mainChainIds)
    {
        var arc = new Arc
        {
            Id = id,
            TitleKey = $"arc.{id}.title",
            Order = order,
            MainChainIds = new List<string>(mainChainIds)
        };
        return SystemJson.JsonSerializer.Serialize(arc);
    }

    [Fact]
    public void Seed_LoadsArcsIntoDatabase()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/arcs")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/arcs", "*.json", false)).Returns(new[] { "arc_1.json" });
        _fsMock.Setup(f => f.ReadAllText("arc_1.json")).Returns(MakeArcJson("arc_1", 1, "beat_1"));

        new ArcSeeder(_fsMock.Object).Seed(_db);

        var col = _db.GetCollection<Arc>("arcs");
        Assert.Equal(1, col.Count());
        var loaded = col.FindAll().First();
        Assert.Equal("arc_1", loaded.Id);
        Assert.Equal("beat_1", loaded.MainChainIds.Single());
    }

    [Fact]
    public void Seed_DuplicateRun_DoesNotDuplicate()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/arcs")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/arcs", "*.json", false)).Returns(new[] { "arc_1.json" });
        _fsMock.Setup(f => f.ReadAllText("arc_1.json")).Returns(MakeArcJson("arc_1", 1, "beat_1"));

        var seeder = new ArcSeeder(_fsMock.Object);
        seeder.Seed(_db);
        seeder.Seed(_db);

        Assert.Equal(1, _db.GetCollection<Arc>("arcs").Count());
    }

    [Fact]
    public void Seed_RemovesOrphanedArcs()
    {
        // First run seeds arc_1 and arc_2.
        _fsMock.Setup(f => f.DirectoryExists("assets/data/arcs")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/arcs", "*.json", false)).Returns(new[] { "arc_1.json", "arc_2.json" });
        _fsMock.Setup(f => f.ReadAllText("arc_1.json")).Returns(MakeArcJson("arc_1", 1, "beat_1"));
        _fsMock.Setup(f => f.ReadAllText("arc_2.json")).Returns(MakeArcJson("arc_2", 2, "beat_2"));

        var seeder = new ArcSeeder(_fsMock.Object);
        seeder.Seed(_db);
        Assert.Equal(2, _db.GetCollection<Arc>("arcs").Count());

        // Second run drops arc_2 from disk — it should be removed from the DB.
        _fsMock.Setup(f => f.GetFiles("assets/data/arcs", "*.json", false)).Returns(new[] { "arc_1.json" });
        seeder.Seed(_db);

        var col = _db.GetCollection<Arc>("arcs");
        Assert.Equal(1, col.Count());
        Assert.Equal("arc_1", col.FindAll().Single().Id);
    }

    [Fact]
    public void Seed_MissingDirectory_DoesNotThrowAndSeedsNothing()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/arcs")).Returns(false);

        var seeder = new ArcSeeder(_fsMock.Object);
        var ex = Record.Exception(() => seeder.Seed(_db));

        Assert.Null(ex);
        Assert.Equal(0, _db.GetCollection<Arc>("arcs").Count());
    }

    [Fact]
    public void Seed_EmptyDirectory_SeedsNothing()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/arcs")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/arcs", "*.json", false)).Returns(Array.Empty<string>());

        new ArcSeeder(_fsMock.Object).Seed(_db);

        Assert.Equal(0, _db.GetCollection<Arc>("arcs").Count());
    }

    [Fact]
    public void Seed_MalformedJson_SkipsFileAndContinues()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/arcs")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/arcs", "*.json", false)).Returns(new[] { "bad.json", "good.json" });
        _fsMock.Setup(f => f.ReadAllText("bad.json")).Returns("{ not valid }");
        _fsMock.Setup(f => f.ReadAllText("good.json")).Returns(MakeArcJson("arc_1", 1, "beat_1"));

        new ArcSeeder(_fsMock.Object).Seed(_db);

        Assert.Equal(1, _db.GetCollection<Arc>("arcs").Count());
    }
}
