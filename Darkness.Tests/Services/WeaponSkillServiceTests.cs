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
        _db = new LiteDatabase(new System.IO.MemoryStream(), new BsonMapper());
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

    [Fact]
    public void GetEquippedSkills_PrioritizesActiveSkillSlots()
    {
        // Arrange
        var skills = _db.GetCollection<Skill>("skills");
        skills.Insert(new Skill { Id = 10, Name = "Active Skill 1" });
        skills.Insert(new Skill { Id = 20, Name = "Active Skill 2" });
        skills.Insert(new Skill { Id = 1, Name = "Slash", WeaponRequirement = "Sword" });

        var character = new Character
        {
            WeaponType = "Sword",
            ActiveSkillSlots = new List<int> { 10, 20, 0, 0, 0 }
        };

        // Act
        var equipped = _service.GetEquippedSkills(character);

        // Assert
        Assert.Equal(2, equipped.Count);
        Assert.Contains(equipped, s => s.Id == 10);
        Assert.Contains(equipped, s => s.Id == 20);
        Assert.DoesNotContain(equipped, s => s.Name == "Slash");
    }
}
