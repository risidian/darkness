namespace Darkness.Core.Models;

public class LevelUpResult
{
    public int XpAwarded { get; set; }
    public int TotalXp { get; set; }
    public int PreviousLevel { get; set; }
    public int NewLevel { get; set; }
    public bool DidLevelUp => NewLevel > PreviousLevel;
    public int LevelsGained => NewLevel - PreviousLevel;
    public int AttributePointsAwarded { get; set; }
}
