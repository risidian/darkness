using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;

namespace Darkness.Tests.Services;

public class LevelSeederTests : IDisposable
{
    private readonly Mock<IFileSystemService> _fsMock;
    private readonly string _dbPath;
    private readonly LiteDatabase _db;

    public LevelSeederTests()
    {
        _fsMock = new Mock<IFileSystemService>();
        _dbPath = Path.Combine(Path.GetTempPath(), $"LevelSeederTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath, new BsonMapper());
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void Seed_LoadsLevelsIntoDatabase()
    {
        var json = "[{\"Value\":1,\"ExperienceRequired\":0},{\"Value\":2,\"ExperienceRequired\":100}]";
        _fsMock.Setup(f => f.ReadAllText("assets/data/level-table.json")).Returns(json);

        var seeder = new LevelSeeder(_fsMock.Object);
        seeder.Seed(_db);

        var col = _db.GetCollection<Level>("levels");
        Assert.Equal(2, col.Count());
    }

    [Fact]
    public void Seed_DuplicateRun_DoesNotDuplicate()
    {
        var json = "[{\"Value\":1,\"ExperienceRequired\":0}]";
        _fsMock.Setup(f => f.ReadAllText("assets/data/level-table.json")).Returns(json);

        var seeder = new LevelSeeder(_fsMock.Object);
        seeder.Seed(_db);
        seeder.Seed(_db);

        Assert.Equal(1, _db.GetCollection<Level>("levels").Count());
    }

    [Fact]
    public void Seed_MissingFile_DoesNotThrow()
    {
        _fsMock.Setup(f => f.ReadAllText("assets/data/level-table.json"))
               .Throws(new FileNotFoundException());

        var seeder = new LevelSeeder(_fsMock.Object);
        var ex = Record.Exception(() => seeder.Seed(_db));
        Assert.Null(ex);
    }
}
