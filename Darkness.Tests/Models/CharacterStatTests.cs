using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models;

public class CharacterStatTests
{
    [Fact]
    public void RecalculateDerivedStats_IncludesTalentArmor() {
        var character = new Character { BaseConstitution = 10 };
        character.TalentStatBonuses["ArmorClass"] = 5;
        character.RecalculateDerivedStats();
        Assert.Equal(10, character.ArmorClass); // Base(10/2) + 5
    }

    [Fact]
    public void RecalculateDerivedStats_IncludesTalentAccuracy() {
        var character = new Character { BaseDexterity = 10 };
        character.TalentStatBonuses["Accuracy"] = 5;
        character.RecalculateDerivedStats();
        Assert.Equal(90, character.Accuracy); // 80 + (10/2) + 5
    }

    [Fact]
    public void RecalculateDerivedStats_IncludesTalentEvasion() {
        var character = new Character { BaseDexterity = 10 };
        character.TalentStatBonuses["Evasion"] = 5;
        character.RecalculateDerivedStats();
        Assert.Equal(10, character.Evasion); // (10/2) + 5
    }

    [Fact]
    public void RecalculateDerivedStats_SumsGearAndTalentBonuses() {
        var character = new Character { BaseConstitution = 10 };
        character.StatBonuses["ArmorClass"] = 5;
        character.TalentStatBonuses["ArmorClass"] = 3;
        character.RecalculateDerivedStats();
        // Base(10/2) + Gear(5) + Talent(3) = 5 + 5 + 3 = 13
        Assert.Equal(13, character.ArmorClass);
    }
}
