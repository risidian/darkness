using System;
using System.Collections.Generic;
using System.IO;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Moq;
using Xunit;

namespace Darkness.Tests.Services;

public class EncounterServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly LiteDatabase _db;
    private readonly Mock<IFileSystemService> _fsMock;
    private readonly EncounterSeeder _seeder;
    private readonly EncounterService _service;

    public EncounterServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"EncounterServiceTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);
        _fsMock = new Mock<IFileSystemService>();
        _seeder = new EncounterSeeder(_fsMock.Object);
        _service = new EncounterService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public void Seed_LoadsDataFromFilesystem()
    {
        // Arrange
        var json = @"[
            {
                ""BackgroundKey"": ""test_bg"",
                ""Encounters"": [
                    { ""Weight"": 100, ""Combat"": { ""BackgroundKey"": ""combat_bg"" } }
                ]
            }
        ]";
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(json);

        // Act
        _seeder.Seed(_db);

        // Assert
        var col = _db.GetCollection<EncounterTable>("encounter_tables");
        Assert.Equal(1, col.Count());
        var table = col.FindOne(t => t.BackgroundKey == "test_bg");
        Assert.NotNull(table);
        Assert.Single(table.Encounters);
        Assert.Equal("combat_bg", table.Encounters[0].Combat.BackgroundKey);
    }

    [Fact]
    public void GetRandomEncounter_ReturnsCorrectEncounter()
    {
        // Arrange
        var table = new EncounterTable
        {
            BackgroundKey = "forest",
            Encounters = new List<EncounterEntry>
            {
                new EncounterEntry { Weight = 100, Combat = new CombatData { BackgroundKey = "forest_battle" } }
            }
        };
        _db.GetCollection<EncounterTable>("encounter_tables").Insert(table);

        // Act
        var result = _service.GetRandomEncounter("forest");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("forest_battle", result.BackgroundKey);
    }

    [Fact]
    public void GetRandomEncounter_ReturnsNullForUnknownBackground()
    {
        // Act
        var result = _service.GetRandomEncounter("unknown");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RollForEncounter_MethodExists()
    {
        // Act
        // This will cause a compilation error until the interface and implementation are updated
        var result = _service.RollForEncounter("forest", 10.0);

        // Assert
        // For Task 2, we just care that it exists. 
        // Logic will be tested in Task 3.
        Assert.Null(result);
    }
}
