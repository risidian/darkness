using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;
using SystemJson = System.Text.Json;

namespace Darkness.Tests.Services;

public class SpriteSeederTests : IDisposable
{
    private readonly Mock<IFileSystemService> _fsMock;
    private readonly string _dbPath;
    private readonly LiteDatabase _db;

    public SpriteSeederTests()
    {
        _fsMock = new Mock<IFileSystemService>();
        _dbPath = Path.Combine(Path.GetTempPath(), $"SpriteSeederTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void Seed_LoadsEquipmentSpritesIntoDatabase()
    {
        var json = SystemJson.JsonSerializer.Serialize(new
        {
            EquipmentSprites = new[]
            {
                new { Slot = "Armor", DisplayName = "Plate (Steel)", AssetPath = "armor/plate",
                      FileNameTemplate = "{action}/steel.png", ZOrder = 60, Gender = "gendered",
                      FallbackGender = (string?)null, TintHex = "#FFFFFF" }
            },
            AppearanceOptions = Array.Empty<object>(),
            ClassDefaults = new Dictionary<string, object>()
        });
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

        var seeder = new SpriteSeeder(_fsMock.Object);
        seeder.Seed(_db);

        var col = _db.GetCollection<EquipmentSprite>("equipment_sprites");
        Assert.Equal(1, col.Count());
        var sprite = col.FindAll().First();
        Assert.Equal("Plate (Steel)", sprite.DisplayName);
        Assert.Equal("Armor", sprite.Slot);
    }

    [Fact]
    public void Seed_LoadsAppearanceOptionsIntoDatabase()
    {
        var json = SystemJson.JsonSerializer.Serialize(new
        {
            EquipmentSprites = Array.Empty<object>(),
            AppearanceOptions = new[]
            {
                new { Category = "Hair", DisplayName = "Long", AssetPath = "hair/long/adult",
                      FileNameTemplate = "{action}/blonde.png", TintHex = "#FFFFFF", ZOrder = 120,
                      Gender = "universal", FallbackGender = (string?)null }
            },
            ClassDefaults = new Dictionary<string, object>()
        });
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

        var seeder = new SpriteSeeder(_fsMock.Object);
        seeder.Seed(_db);

        var col = _db.GetCollection<AppearanceOption>("appearance_options");
        Assert.Equal(1, col.Count());
        Assert.Equal("Long", col.FindAll().First().DisplayName);
    }

    [Fact]
    public void Seed_DuplicateRun_DoesNotCreateDuplicates()
    {
        var json = SystemJson.JsonSerializer.Serialize(new
        {
            EquipmentSprites = new[]
            {
                new { Slot = "Armor", DisplayName = "Plate (Steel)", AssetPath = "armor/plate",
                      FileNameTemplate = "{action}/steel.png", ZOrder = 60, Gender = "gendered",
                      FallbackGender = (string?)null, TintHex = "#FFFFFF" }
            },
            AppearanceOptions = Array.Empty<object>(),
            ClassDefaults = new Dictionary<string, object>()
        });
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

        var seeder = new SpriteSeeder(_fsMock.Object);
        seeder.Seed(_db);
        seeder.Seed(_db);

        var col = _db.GetCollection<EquipmentSprite>("equipment_sprites");
        Assert.Equal(1, col.Count());
    }

    [Fact]
    public void Seed_MissingFile_LogsErrorAndDoesNotThrow()
    {
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json"))
               .Throws(new FileNotFoundException("File not found"));

        var seeder = new SpriteSeeder(_fsMock.Object);
        var ex = Record.Exception(() => seeder.Seed(_db));

        Assert.Null(ex);
        Assert.Equal(0, _db.GetCollection<EquipmentSprite>("equipment_sprites").Count());
    }

    [Fact]
    public void Seed_MalformedJson_LogsErrorAndDoesNotThrow()
    {
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns("{ invalid json");

        var seeder = new SpriteSeeder(_fsMock.Object);
        var ex = Record.Exception(() => seeder.Seed(_db));

        Assert.Null(ex);
    }
}
