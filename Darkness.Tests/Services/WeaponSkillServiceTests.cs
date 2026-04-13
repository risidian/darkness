using System.Collections.Generic;
using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Xunit;

namespace Darkness.Tests.Services;

public class WeaponSkillServiceTests
{
    private readonly WeaponSkillService _service;
    private readonly LiteDatabase _db;

    public WeaponSkillServiceTests()
    {
        _db = new LiteDatabase("Filename=:memory:;");
        _service = new WeaponSkillService(_db);
    }

    [Theory]
    [InlineData("Arming Sword (Steel)", "None", "None", "Slash")]
    [InlineData("Mage Wand", "None", "None", "Arcane Bolt")]
    [InlineData("Dagger (Steel)", "None", "None", "Quick Stab")]
    [InlineData("Waraxe", "None", "None", "Cleave")]
    [InlineData("Mace", "None", "None", "Smash")]
    [InlineData("Recurve Bow", "None", "None", "Quick Shot")]
    [InlineData(null, "None", "None", "Punch")]
    [InlineData("None", "None", "None", "Punch")]
    public void GetSkillsForWeapon_ReturnsCorrectSkills(string? weaponType, string? offHandType, string? shieldType, string expectedFirstSkill)
    {
        var skills = _service.GetSkillsForWeapon(weaponType, offHandType, shieldType);
        Assert.NotEmpty(skills);
        Assert.Equal(expectedFirstSkill, skills[0].Name);
    }

    [Fact]
    public void GetSkillsForWeapon_MageWithDagger_HasBothSkills()
    {
        var skills = _service.GetSkillsForWeapon("Mage Wand", "Dagger (Steel)", "None");
        Assert.Contains(skills, s => s.Name == "Arcane Bolt");
        Assert.Contains(skills, s => s.Name == "Offhand Stab");
    }

    [Fact]
    public void GetSkillsForWeapon_Mace_HasShieldBlock_WhenShieldEquipped()
    {
        var skills = _service.GetSkillsForWeapon("Mace", "None", "Crusader");
        Assert.Contains(skills, s => s.Name == "Shield Block");
    }

    [Fact]
    public void GetSkillsForWeapon_Axe_HasBrace_WhenNoShield()
    {
        var skills = _service.GetSkillsForWeapon("Waraxe", "None", "None");
        Assert.Contains(skills, s => s.Name == "Brace");
    }

    [Fact]
    public void GetSkillsForWeapon_WithUnlockedTalents_ReturnsTalentSkills()
    {
        // Arrange
        var talents = _db.GetCollection<TalentTree>("talent_trees");
        talents.Insert(new TalentTree
        {
            Id = "paladin_tree",
            Nodes = new List<TalentNode>
            {
                new TalentNode
                {
                    Id = "holy_strike_node",
                    Name = "Holy Strike",
                    Description = "A holy strike",
                    Effect = new TalentEffect { Skill = "Holy Strike" }
                }
            }
        });

        var unlockedTalentIds = new List<string> { "holy_strike_node" };

        // Act
        var skills = _service.GetSkillsForWeapon("Sword", "None", "None", unlockedTalentIds);

        // Assert
        Assert.Contains(skills, s => s.Name == "Holy Strike");
        var holyStrike = skills.Find(s => s.Name == "Holy Strike");
        Assert.Equal(1.8f, holyStrike.DamageMultiplier);
    }
}
