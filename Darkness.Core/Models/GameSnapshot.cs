namespace Darkness.Core.Models
{
    public record CharacterSnapshot(
        string Name,
        string Class,
        int CurrentHP,
        int MaxHP,
        int Level,
        byte[]? Thumbnail,
        string HairColor,
        string HairStyle,
        string SkinColor
    );

    public record BattleSnapshot(
        List<CharacterSnapshot> Party,
        List<Enemy> Enemies,
        int? SurvivalTurns
    );
}
