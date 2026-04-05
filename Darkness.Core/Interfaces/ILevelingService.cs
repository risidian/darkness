using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface ILevelingService
{
    LevelUpResult AwardExperience(Character character, int xp);
    int GetXpToNextLevel(Character character);
    int GetLevelForXp(int totalXp);
}
