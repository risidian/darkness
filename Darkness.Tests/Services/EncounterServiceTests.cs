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
                ""EncounterChance"": 15,
                ""EncounterDistance"": 2000,
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
        Assert.Equal(15, table.EncounterChance);
        Assert.Equal(2000f, table.EncounterDistance);
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
    public void RollForEncounter_RespectsDistanceThreshold()
    {
        // Arrange
        var table = new EncounterTable
        {
            BackgroundKey = "test_bg",
            EncounterChance = 100, // Always succeed if distance met
            EncounterDistance = 500f,
            Encounters = new List<EncounterEntry> { new EncounterEntry { Weight = 1, Combat = new CombatData() } }
        };
        _db.GetCollection<EncounterTable>("encounter_tables").Insert(table);

        // Act & Assert
        Assert.Null(_service.RollForEncounter("test_bg", 499)); // Below threshold
        Assert.NotNull(_service.RollForEncounter("test_bg", 501)); // Above threshold
    }

    [Fact]
    public void RollForEncounter_RespectsChance_Failure()
    {
        // Arrange
        var table = new EncounterTable
        {
            BackgroundKey = "low_chance",
            EncounterChance = 0, // Never succeed
            EncounterDistance = 0,
            Encounters = new List<EncounterEntry> { new EncounterEntry { Weight = 1, Combat = new CombatData() } }
        };
        _db.GetCollection<EncounterTable>("encounter_tables").Insert(table);

        // Act
        var result = _service.RollForEncounter("low_chance", 100);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RollForEncounter_ReturnsNullForUnknownBackground()
    {
        // Act
        var result = _service.RollForEncounter("unknown", 1000);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RollForEncounter_ReturnsNullForEmptyEncounters()
    {
        // Arrange
        var table = new EncounterTable
        {
            BackgroundKey = "empty",
            EncounterChance = 100,
            EncounterDistance = 0,
            Encounters = new List<EncounterEntry>()
        };
        _db.GetCollection<EncounterTable>("encounter_tables").Insert(table);

        // Act
        var result = _service.RollForEncounter("empty", 100);

        // Assert
        Assert.Null(result);
    }
}
