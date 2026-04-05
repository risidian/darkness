using System;
using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class LevelingService : ILevelingService
{
    private const int AttributePointsPerLevel = 2;
    private readonly LiteDatabase _db;

    public LevelingService(LiteDatabase db)
    {
        _db = db;
    }

    public LevelUpResult AwardExperience(Character character, int xp)
    {
        int previousLevel = character.Level;
        character.Experience += xp;

        int newLevel = GetLevelForXp(character.Experience);
        int levelsGained = newLevel - previousLevel;
        int pointsAwarded = 0;

        if (levelsGained > 0)
        {
            character.Level = newLevel;
            pointsAwarded = levelsGained * AttributePointsPerLevel;
            character.AttributePoints += pointsAwarded;
            character.CurrentHP = character.MaxHP;
        }

        return new LevelUpResult
        {
            XpAwarded = xp,
            TotalXp = character.Experience,
            PreviousLevel = previousLevel,
            NewLevel = newLevel,
            AttributePointsAwarded = pointsAwarded
        };
    }

    public int GetXpToNextLevel(Character character)
    {
        var levels = GetLevelTable();
        var nextLevel = levels.FirstOrDefault(l => l.Value == character.Level + 1);
        if (nextLevel == null) return 0;
        return Math.Max(0, nextLevel.ExperienceRequired - character.Experience);
    }

    public int GetLevelForXp(int totalXp)
    {
        var levels = GetLevelTable();
        int level = 1;
        foreach (var l in levels.OrderBy(l => l.Value))
        {
            if (totalXp >= l.ExperienceRequired)
                level = l.Value;
            else
                break;
        }
        return level;
    }

    private List<Level> GetLevelTable()
    {
        return _db.GetCollection<Level>("levels").FindAll().ToList();
    }
}
