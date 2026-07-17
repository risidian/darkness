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

/// <summary>
/// TDD tests for the data-driven DeathmatchService + DeathmatchSeeder.
/// Tests are written FIRST; implementation follows to make them pass.
/// </summary>
public class DeathmatchServiceTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly Mock<IFileSystemService> _fsMock;
    private readonly DeathmatchSeeder _seeder;
    private readonly DeathmatchService _service;

    // Full 8-wave JSON that mirrors the real seed file schema
    private const string EightWaveJson = @"[
      { ""Id"": 1, ""Title"": ""The Gauntlet"", ""Description"": ""First wave."", ""Difficulty"": ""Easy"", ""UnlockRequirement"": null,
        ""Enemies"": [{ ""Name"": ""Goblin Scout"", ""Level"": 1, ""MaxHP"": 30, ""Attack"": 5, ""Defense"": 2, ""ExperienceReward"": 25 }] },
      { ""Id"": 2, ""Title"": ""The Onslaught"", ""Description"": ""Second wave."", ""Difficulty"": ""Easy"", ""UnlockRequirement"": 1,
        ""Enemies"": [{ ""Name"": ""Goblin Warrior"", ""Level"": 2, ""MaxHP"": 40, ""Attack"": 7, ""Defense"": 3, ""ExperienceReward"": 40 }] },
      { ""Id"": 3, ""Title"": ""The Siege"", ""Description"": ""Third wave."", ""Difficulty"": ""Normal"", ""UnlockRequirement"": 2,
        ""Enemies"": [{ ""Name"": ""Orc Scout"", ""Level"": 4, ""MaxHP"": 60, ""Attack"": 10, ""Defense"": 5, ""ExperienceReward"": 60 }] },
      { ""Id"": 4, ""Title"": ""The Crucible"", ""Description"": ""Fourth wave."", ""Difficulty"": ""Normal"", ""UnlockRequirement"": 3,
        ""Enemies"": [{ ""Name"": ""Orc Grunt"", ""Level"": 5, ""MaxHP"": 75, ""Attack"": 12, ""Defense"": 6, ""ExperienceReward"": 80 }] },
      { ""Id"": 5, ""Title"": ""The Reckoning"", ""Description"": ""Fifth wave."", ""Difficulty"": ""Hard"", ""UnlockRequirement"": 4,
        ""Enemies"": [{ ""Name"": ""Dark Knight"", ""Level"": 7, ""MaxHP"": 100, ""Attack"": 16, ""Defense"": 9, ""ExperienceReward"": 120 }] },
      { ""Id"": 6, ""Title"": ""The Abyss"", ""Description"": ""Sixth wave."", ""Difficulty"": ""Hard"", ""UnlockRequirement"": 5,
        ""Enemies"": [{ ""Name"": ""Shadow Wraith"", ""Level"": 9, ""MaxHP"": 130, ""Attack"": 20, ""Defense"": 11, ""ExperienceReward"": 160 }] },
      { ""Id"": 7, ""Title"": ""The Apocalypse"", ""Description"": ""Seventh wave."", ""Difficulty"": ""Expert"", ""UnlockRequirement"": 6,
        ""Enemies"": [{ ""Name"": ""Demon Lord"", ""Level"": 12, ""MaxHP"": 180, ""Attack"": 28, ""Defense"": 15, ""ExperienceReward"": 250 }] },
      { ""Id"": 8, ""Title"": ""The Omega Trial"", ""Description"": ""Final wave."", ""Difficulty"": ""Legendary"", ""UnlockRequirement"": 7,
        ""Enemies"": [{ ""Name"": ""Dragon Overlord"", ""Level"": 15, ""MaxHP"": 250, ""Attack"": 40, ""Defense"": 20, ""ExperienceReward"": 400 }] }
    ]";

    public DeathmatchServiceTests()
    {
        _db = new LiteDatabase(new MemoryStream());
        _fsMock = new Mock<IFileSystemService>();
        _seeder = new DeathmatchSeeder(_fsMock.Object);
        _service = new DeathmatchService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ─── Seeder Tests ────────────────────────────────────────────────────────

    [Fact]
    public void Seed_Loads8WavesIntoDatabase()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);

        // Act
        _seeder.Seed(_db);

        // Assert
        var col = _db.GetCollection<DeathmatchEncounter>("deathmatch_encounters");
        Assert.Equal(8, col.Count());
    }

    [Fact]
    public void Seed_IsIdempotent_RunningTwiceDoesNotDuplicate()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);

        // Act — seed twice
        _seeder.Seed(_db);
        _seeder.Seed(_db);

        // Assert — still only 8
        var col = _db.GetCollection<DeathmatchEncounter>("deathmatch_encounters");
        Assert.Equal(8, col.Count());
    }

    [Fact]
    public void Seed_DoesNotCrash_WhenFileNotFound()
    {
        // Arrange — file does not exist
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        // Act & Assert — no exception thrown
        var ex = Record.Exception(() => _seeder.Seed(_db));
        Assert.Null(ex);
    }

    [Fact]
    public void Seed_DoesNotCrash_WhenJsonIsInvalid()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns("this is not JSON {{{{");

        // Act & Assert
        var ex = Record.Exception(() => _seeder.Seed(_db));
        Assert.Null(ex);
    }

    [Fact]
    public void Seed_StoresTitle_Description_Difficulty_Correctly()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);

        // Act
        _seeder.Seed(_db);

        // Assert
        var col = _db.GetCollection<DeathmatchEncounter>("deathmatch_encounters");
        var wave1 = col.FindById(1);
        Assert.NotNull(wave1);
        Assert.Equal("The Gauntlet", wave1.Title);
        Assert.Equal("First wave.", wave1.Description);
        Assert.Equal("Easy", wave1.Difficulty);
        Assert.Null(wave1.UnlockRequirement);
    }

    [Fact]
    public void Seed_StoresUnlockRequirement_ForWave8()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);

        // Act
        _seeder.Seed(_db);

        // Assert
        var col = _db.GetCollection<DeathmatchEncounter>("deathmatch_encounters");
        var wave8 = col.FindById(8);
        Assert.NotNull(wave8);
        Assert.Equal(7, wave8.UnlockRequirement);
    }

    // ─── GetEncountersAsync Tests ─────────────────────────────────────────────

    [Fact]
    public async System.Threading.Tasks.Task GetEncountersAsync_Returns8Waves_AfterSeeding()
    {
        // Arrange — seed directly into the in-memory DB
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);
        _seeder.Seed(_db);

        // Act
        var result = await _service.GetEncountersAsync();

        // Assert
        Assert.Equal(8, result.Count);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetEncountersAsync_Wave1_AlwaysUnlocked_ForFreshCharacter()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);
        _seeder.Seed(_db);
        var freshCharacter = new Character { Id = 1, Name = "Hero", ClearedDeathmatchWaves = new List<int>() };

        // Act
        var result = await _service.GetEncountersAsync(freshCharacter);
        var wave1 = result.Find(e => e.Id == 1);

        // Assert — wave 1 has no unlock requirement; always unlocked
        Assert.NotNull(wave1);
        Assert.True(wave1.IsUnlocked);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetEncountersAsync_Wave2_LockedForFreshCharacter()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);
        _seeder.Seed(_db);
        var freshCharacter = new Character { Id = 1, Name = "Hero", ClearedDeathmatchWaves = new List<int>() };

        // Act
        var result = await _service.GetEncountersAsync(freshCharacter);
        var wave2 = result.Find(e => e.Id == 2);

        // Assert — wave 2 requires wave 1 cleared; fresh character has none
        Assert.NotNull(wave2);
        Assert.False(wave2.IsUnlocked);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetEncountersAsync_Wave2_UnlockedWhenWave1Cleared()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);
        _seeder.Seed(_db);
        var character = new Character { Id = 1, Name = "Hero", ClearedDeathmatchWaves = new List<int> { 1 } };

        // Act
        var result = await _service.GetEncountersAsync(character);
        var wave2 = result.Find(e => e.Id == 2);

        // Assert
        Assert.NotNull(wave2);
        Assert.True(wave2.IsUnlocked);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetEncountersAsync_Wave8_RequiresWave7Cleared()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);
        _seeder.Seed(_db);

        // Character has cleared waves 1-6 but NOT wave 7
        var character = new Character { Id = 1, Name = "Hero", ClearedDeathmatchWaves = new List<int> { 1, 2, 3, 4, 5, 6 } };

        // Act
        var result = await _service.GetEncountersAsync(character);
        var wave8 = result.Find(e => e.Id == 8);

        // Assert — wave 8 requires 7 cleared; not yet cleared
        Assert.NotNull(wave8);
        Assert.False(wave8.IsUnlocked);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetEncountersAsync_Wave8_UnlockedWhenWave7Cleared()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);
        _seeder.Seed(_db);

        var character = new Character { Id = 1, Name = "Hero", ClearedDeathmatchWaves = new List<int> { 1, 2, 3, 4, 5, 6, 7 } };

        // Act
        var result = await _service.GetEncountersAsync(character);
        var wave8 = result.Find(e => e.Id == 8);

        // Assert
        Assert.NotNull(wave8);
        Assert.True(wave8.IsUnlocked);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetEncountersAsync_NullCharacter_Wave1UnlockedOnly()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);
        _seeder.Seed(_db);

        // Act — no character passed; null default
        var result = await _service.GetEncountersAsync(null);
        var wave1 = result.Find(e => e.Id == 1);
        var wave2 = result.Find(e => e.Id == 2);

        // Assert
        Assert.NotNull(wave1);
        Assert.True(wave1.IsUnlocked);
        Assert.NotNull(wave2);
        Assert.False(wave2.IsUnlocked);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetEncountersAsync_AllWaves_UnlockedWhenAllPreviousCleared()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);
        _seeder.Seed(_db);

        // Character that has cleared all 8 waves
        var character = new Character { Id = 1, Name = "Legend", ClearedDeathmatchWaves = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 } };

        // Act
        var result = await _service.GetEncountersAsync(character);

        // Assert — all 8 unlocked
        Assert.All(result, e => Assert.True(e.IsUnlocked, $"Wave {e.Id} should be unlocked"));
    }

    [Fact]
    public async System.Threading.Tasks.Task GetEncountersAsync_AllEncountersHaveEnemies()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);
        _seeder.Seed(_db);

        // Act
        var result = await _service.GetEncountersAsync();

        // Assert — every wave must have at least one enemy
        Assert.All(result, e => Assert.NotEmpty(e.Enemies));
    }

    [Fact]
    public async System.Threading.Tasks.Task GetEncountersAsync_AllEncountersHaveTitle()
    {
        // Arrange
        _fsMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(EightWaveJson);
        _seeder.Seed(_db);

        // Act
        var result = await _service.GetEncountersAsync();

        // Assert
        Assert.All(result, e => Assert.False(string.IsNullOrWhiteSpace(e.Title)));
    }
}
