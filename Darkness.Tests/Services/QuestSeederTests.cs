using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;
using SystemJson = System.Text.Json;

namespace Darkness.Tests.Services;

public class QuestSeederTests : IDisposable
{
    private readonly Mock<IFileSystemService> _fsMock;
    private readonly string _dbPath;
    private readonly LiteDatabase _db;

    public QuestSeederTests()
    {
        _fsMock = new Mock<IFileSystemService>();
        _dbPath = Path.Combine(Path.GetTempPath(), $"QuestSeederTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath, new BsonMapper());
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    private string MakeChainJson(string id, string title, int sortOrder = 1)
    {
        var chain = new QuestChain
        {
            Id = id, Title = title, IsMainStory = true, SortOrder = sortOrder,
            Steps = new List<QuestStep>
            {
                new() { Id = $"{id}_step1", Type = "dialogue",
                    Dialogue = new DialogueData { Speaker = "NPC", Lines = new() { "Hello" } } }
            }
        };
        return SystemJson.JsonSerializer.Serialize(chain);
    }

    [Fact]
    public void Seed_LoadsChainsIntoDatabase()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/quests")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/quests", "*.json")).Returns(new[] { "beat_1.json" });
        _fsMock.Setup(f => f.ReadAllText("beat_1.json")).Returns(MakeChainJson("beat_1", "The Awakening"));

        var seeder = new QuestSeeder(_fsMock.Object);
        seeder.Seed(_db);

        var col = _db.GetCollection<QuestChain>("quest_chains");
        Assert.Equal(1, col.Count());
        Assert.Equal("beat_1", col.FindAll().First().Id);
    }

    [Fact]
    public void Seed_DuplicateRun_DoesNotDuplicate()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/quests")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/quests", "*.json")).Returns(new[] { "beat_1.json" });
        _fsMock.Setup(f => f.ReadAllText("beat_1.json")).Returns(MakeChainJson("beat_1", "The Awakening"));

        var seeder = new QuestSeeder(_fsMock.Object);
        seeder.Seed(_db);
        seeder.Seed(_db);

        Assert.Equal(1, _db.GetCollection<QuestChain>("quest_chains").Count());
    }

    [Fact]
    public void Seed_MissingDirectory_LogsAndDoesNotThrow()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/quests")).Returns(false);

        var seeder = new QuestSeeder(_fsMock.Object);
        var ex = Record.Exception(() => seeder.Seed(_db));
        Assert.Null(ex);
    }

    [Fact]
    public void Seed_MalformedJson_SkipsFileAndContinues()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/quests")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/quests", "*.json")).Returns(new[] { "bad.json", "good.json" });
        _fsMock.Setup(f => f.ReadAllText("bad.json")).Returns("{ not valid }");
        _fsMock.Setup(f => f.ReadAllText("good.json")).Returns(MakeChainJson("beat_1", "Good Chain"));

        var seeder = new QuestSeeder(_fsMock.Object);
        seeder.Seed(_db);

        Assert.Equal(1, _db.GetCollection<QuestChain>("quest_chains").Count());
    }

    [Fact]
    public void Seed_MultipleFiles_LoadsAll()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/quests")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/quests", "*.json")).Returns(new[] { "a.json", "b.json" });
        _fsMock.Setup(f => f.ReadAllText("a.json")).Returns(MakeChainJson("beat_1", "Chain A", 1));
        _fsMock.Setup(f => f.ReadAllText("b.json")).Returns(MakeChainJson("beat_2", "Chain B", 2));

        var seeder = new QuestSeeder(_fsMock.Object);
        seeder.Seed(_db);

        Assert.Equal(2, _db.GetCollection<QuestChain>("quest_chains").Count());
    }

    [Fact]
    public void Regression_AllLiveDataFiles_AreValidJson()
    {
        // This test recursively checks ALL JSON files in the assets/data directory
        // to ensure no syntax errors exist in any game data.
        string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../Darkness.Godot/assets/data");
        
        if (!Directory.Exists(dataDir))
        {
            dataDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../Darkness.Godot/assets/data"));
        }

        Assert.True(Directory.Exists(dataDir), $"Data directory not found at: {dataDir}");

        // Get all JSON files recursively
        var files = Directory.GetFiles(dataDir, "*.json", SearchOption.AllDirectories);
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            try
            {
                // Use JsonDocument to validate syntax without needing a specific model
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                Assert.NotNull(doc);
            }
            catch (System.Text.Json.JsonException ex)
            {
                Assert.Fail($"JSON Syntax Error in {Path.GetFileName(file)}: {ex.Message}\nPath: {file}");
            }
        }
    }
}
