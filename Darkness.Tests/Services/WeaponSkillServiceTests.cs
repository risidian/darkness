using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Moq;
using Xunit;

namespace Darkness.Tests.Services;

public class WeaponSkillServiceTests
{
    private readonly WeaponSkillService _service;
    private readonly LiteDatabase _db;
    private readonly Mock<IFileSystemService> _mockFs;

    public WeaponSkillServiceTests()
    {
        _db = new LiteDatabase(new System.IO.MemoryStream(), new BsonMapper());
        _mockFs = new Mock<IFileSystemService>();
        
        // Default mock setup to return basic skills
        var skillsJson = @"[
            { ""Id"": 1, ""Name"": ""Slash"", ""WeaponRequirement"": ""Sword"", ""SkillType"": ""Physical"", ""DamageMultiplier"": 1.2 },
            { ""Id"": 2, ""Name"": ""Arcane Bolt"", ""WeaponRequirement"": ""Wand"", ""SkillType"": ""Magical"", ""DamageMultiplier"": 1.1 },
            { ""Id"": 3, ""Name"": ""Quick Stab"", ""WeaponRequirement"": ""Dagger"", ""SkillType"": ""Physical"", ""DamageMultiplier"": 1.0 },
            { ""Id"": 4, ""Name"": ""Cleave"", ""WeaponRequirement"": ""Axe"", ""SkillType"": ""Physical"", ""DamageMultiplier"": 1.0 },
            { ""Id"": 5, ""Name"": ""Smash"", ""WeaponRequirement"": ""Mace"", ""SkillType"": ""Physical"", ""DamageMultiplier"": 1.1 },
            { ""Id"": 6, ""Name"": ""Quick Shot"", ""WeaponRequirement"": ""Bow"", ""SkillType"": ""Physical"", ""DamageMultiplier"": 1.0 },
            { ""Id"": 7, ""Name"": ""Punch"", ""WeaponRequirement"": ""None"", ""SkillType"": ""Physical"", ""DamageMultiplier"": 0.5 },
            { ""Id"": 8, ""Name"": ""Shield Block"", ""WeaponRequirement"": ""Shield"", ""SkillType"": ""Defensive"", ""BlockReduction"": 0.6 },
            { ""Id"": 9, ""Name"": ""Brace"", ""WeaponRequirement"": ""None"", ""SkillType"": ""Defensive"", ""BlockReduction"": 0.2 },
            { ""Id"": 21, ""Name"": ""Holy Strike"", ""WeaponRequirement"": ""None"", ""SkillType"": ""Magical"", ""DamageMultiplier"": 1.8, ""TalentRequirement"": ""holy_strike_node"" },
            { ""Id"": 22, ""Name"": ""Offhand Stab"", ""WeaponRequirement"": ""Dagger"", ""SkillType"": ""Physical"", ""DamageMultiplier"": 1.0, ""IsOffHand"": true }
        ]";
        _mockFs.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(skillsJson);

        _service = new WeaponSkillService(_db, _mockFs.Object);
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
