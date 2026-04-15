using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;
using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Darkness.Tests.Services;

public class AppearanceSeederTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly Mock<IFileSystemService> _fsMock;

    public AppearanceSeederTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"AppearanceSeederTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath, new BsonMapper());
        _fsMock = new Mock<IFileSystemService>();
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void Seed_LoadsAppearanceOptionsFromCatalog()
    {
        // 1. Setup mock JSON
        var json = @"{
            ""AppearanceOptions"": [
                { ""Category"": ""Hair"", ""DisplayName"": ""Long"", ""AssetPath"": ""hair/long"" },
                { ""Category"": ""Skin"", ""DisplayName"": ""Amber"", ""AssetPath"": ""skin/amber"" }
            ]
        }";
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

        // 2. Run seeder
        var seeder = new AppearanceSeeder(_fsMock.Object);
        seeder.Seed(_db);

        // 3. Assert
        var col = _db.GetCollection<AppearanceOption>("appearance_options");
        Assert.Equal(2, col.Count());
        Assert.NotNull(col.FindOne(x => x.DisplayName == "Long"));
    }
}
