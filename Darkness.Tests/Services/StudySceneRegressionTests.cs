using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Services;

/// <summary>
/// Regression tests for the stat inflation bug in StudyScene.
/// The bug: StudyScene was writing to computed property Strength (which had a
/// setter routing to BaseStrength) instead of BaseStrength directly. Now the
/// computed properties are expression-bodied (no setter), preventing this class
/// of bug from recurring.
/// </summary>
public class StudySceneRegressionTests
{
    [Fact]
    public void ComputedStrength_IncludesStatBonus()
    {
        var character = new Character
        {
            BaseStrength = 10,
            StatBonuses = new Dictionary<string, int> { { "Strength", 5 } }
        };

        Assert.Equal(10, character.BaseStrength);
        Assert.Equal(15, character.Strength);
    }

    [Fact]
    public void ModifyingBaseStrength_UpdatesComputedCorrectly()
    {
        var character = new Character
        {
            BaseStrength = 10,
            StatBonuses = new Dictionary<string, int> { { "Strength", 5 } }
        };

        character.BaseStrength += 1;

        Assert.Equal(11, character.BaseStrength);
        Assert.Equal(16, character.Strength);
    }

    [Fact]
    public void RecalculateDerivedStats_UsesComputedStrengthWithBonus()
    {
        var character = new Character
        {
            BaseStrength = 10,
            BaseConstitution = 10,
            BaseWisdom = 10,
            BaseDexterity = 10,
            StatBonuses = new Dictionary<string, int> { { "Strength", 5 } },
            Level = 1
        };

        character.RecalculateDerivedStats();

        // Attack uses computed Strength (15), not BaseStrength (10)
        // Attack = Strength * 2 + GetTotalBonus("Attack") = 15 * 2 + 0 = 30
        Assert.Equal(30, character.Attack);

        // CarryCapacity uses computed Strength (15)
        // CarryCapacity = Strength * 20 = 15 * 20 = 300
        Assert.Equal(300, character.CarryCapacity);
    }

    [Fact]
    public void TalentStatBonuses_AlsoFlowIntoComputedStats()
    {
        var character = new Character
        {
            BaseStrength = 10,
            BaseConstitution = 10,
            BaseWisdom = 10,
            BaseDexterity = 10,
            StatBonuses = new Dictionary<string, int> { { "Strength", 5 } },
            TalentStatBonuses = new Dictionary<string, int> { { "Strength", 3 } },
            Level = 1
        };

        // Computed: 10 + 5 + 3 = 18
        Assert.Equal(18, character.Strength);

        character.RecalculateDerivedStats();

        // Attack = 18 * 2 = 36
        Assert.Equal(36, character.Attack);
    }

    [Fact]
    public void AllComputedStats_IncludeBothBonusSources()
    {
        var character = new Character
        {
            BaseStrength = 10,
            BaseDexterity = 12,
            BaseConstitution = 14,
            BaseIntelligence = 8,
            BaseWisdom = 11,
            BaseCharisma = 9,
            StatBonuses = new Dictionary<string, int>
            {
                { "Strength", 2 },
                { "Dexterity", 3 },
                { "Constitution", 1 },
                { "Intelligence", 4 },
                { "Wisdom", 2 },
                { "Charisma", 1 }
            },
            TalentStatBonuses = new Dictionary<string, int>
            {
                { "Strength", 1 },
                { "Dexterity", 1 }
            }
        };

        Assert.Equal(13, character.Strength);       // 10 + 2 + 1
        Assert.Equal(16, character.Dexterity);       // 12 + 3 + 1
        Assert.Equal(15, character.Constitution);    // 14 + 1 + 0
        Assert.Equal(12, character.Intelligence);    // 8 + 4 + 0
        Assert.Equal(13, character.Wisdom);          // 11 + 2 + 0
        Assert.Equal(10, character.Charisma);        // 9 + 1 + 0
    }
}
