using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;

namespace Darkness.Tests.Services;

public class LevelingServiceTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly LevelingService _levelingService;

    public LevelingServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"LevelingServiceTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath, new BsonMapper());

        var levels = _db.GetCollection<Level>("levels");
        levels.Insert(new Level { Value = 1, ExperienceRequired = 0 });
        levels.Insert(new Level { Value = 2, ExperienceRequired = 100 });
        levels.Insert(new Level { Value = 3, ExperienceRequired = 250 });
        levels.Insert(new Level { Value = 4, ExperienceRequired = 500 });
        levels.Insert(new Level { Value = 5, ExperienceRequired = 900 });

        _levelingService = new LevelingService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void AwardExperience_IncreasesCharacterXp()
    {
        var character = new Character { Level = 1, Experience = 0, MaxHP = 50, CurrentHP = 50 };
        var result = _levelingService.AwardExperience(character, 50);
        Assert.Equal(50, character.Experience);
        Assert.Equal(50, result.XpAwarded);
        Assert.Equal(50, result.TotalXp);
        Assert.False(result.DidLevelUp);
    }

    [Fact]
    public void AwardExperience_TriggersLevelUp_WhenThresholdCrossed()
    {
        var character = new Character { Level = 1, Experience = 0, MaxHP = 50, CurrentHP = 30, AttributePoints = 0 };
        var result = _levelingService.AwardExperience(character, 100);
        Assert.True(result.DidLevelUp);
        Assert.Equal(2, character.Level);
        Assert.Equal(2, result.NewLevel);
        Assert.Equal(1, result.PreviousLevel);
        Assert.Equal(2, result.AttributePointsAwarded);
    }

    [Fact]
    public void AwardExperience_RestoresHpOnLevelUp()
    {
        var character = new Character { Level = 1, Experience = 0, MaxHP = 50, CurrentHP = 10 };
        _levelingService.AwardExperience(character, 100);
        Assert.Equal(50, character.CurrentHP);
    }

    [Fact]
    public void AwardExperience_MultiLevelUp_AwardsCorrectPoints()
    {
        var character = new Character { Level = 1, Experience = 0, MaxHP = 50, CurrentHP = 50, AttributePoints = 0 };
        var result = _levelingService.AwardExperience(character, 500);
        Assert.Equal(4, character.Level);
        Assert.Equal(3, result.LevelsGained);
        Assert.Equal(6, result.AttributePointsAwarded);
    }

    [Fact]
    public void AwardExperience_NoLevelUp_DoesNotRestoreHp()
    {
        var character = new Character { Level = 1, Experience = 0, MaxHP = 50, CurrentHP = 30 };
        _levelingService.AwardExperience(character, 50);
        Assert.Equal(30, character.CurrentHP);
    }

    [Fact]
    public void GetXpToNextLevel_ReturnsCorrectRemaining()
    {
        var character = new Character { Level = 1, Experience = 60 };
        var remaining = _levelingService.GetXpToNextLevel(character);
        Assert.Equal(40, remaining);
    }

    [Fact]
    public void GetXpToNextLevel_AtMaxLevel_ReturnsZero()
    {
        var character = new Character { Level = 5, Experience = 9999 };
        var remaining = _levelingService.GetXpToNextLevel(character);
        Assert.Equal(0, remaining);
    }

    [Fact]
    public void GetLevelForXp_ReturnsCorrectLevel()
    {
        Assert.Equal(1, _levelingService.GetLevelForXp(0));
        Assert.Equal(1, _levelingService.GetLevelForXp(99));
        Assert.Equal(2, _levelingService.GetLevelForXp(100));
        Assert.Equal(3, _levelingService.GetLevelForXp(250));
        Assert.Equal(5, _levelingService.GetLevelForXp(10000));
    }

    [Fact]
    public void AwardExperience_EvenLevels_AwardsTalentPoints()
    {
        var character = new Character { Level = 1, Experience = 0, TalentPoints = 0 };
        
        // Level 1 -> 2 (Even)
        var result = _levelingService.AwardExperience(character, 100);
        Assert.Equal(2, character.Level);
        Assert.Equal(1, character.TalentPoints);
        Assert.Equal(1, result.TalentPointsAwarded);

        // Level 2 -> 3 (Odd)
        result = _levelingService.AwardExperience(character, 150);
        Assert.Equal(3, character.Level);
        Assert.Equal(1, character.TalentPoints);
        Assert.Equal(0, result.TalentPointsAwarded);

        // Level 3 -> 4 (Even)
        result = _levelingService.AwardExperience(character, 250);
        Assert.Equal(4, character.Level);
        Assert.Equal(2, character.TalentPoints);
        Assert.Equal(1, result.TalentPointsAwarded);
    }

    [Fact]
    public void AwardExperience_MultiLevel_AwardsCorrectTalentPoints()
    {
        var character = new Character { Level = 1, Experience = 0, TalentPoints = 0 };
        
        // Level 1 -> 4 (2 and 4 are even)
        var result = _levelingService.AwardExperience(character, 500);
        Assert.Equal(4, character.Level);
        Assert.Equal(2, character.TalentPoints);
        Assert.Equal(2, result.TalentPointsAwarded);
    }
}
