using System.Collections.Generic;
using Darkness.Core.Models;
using Darkness.Core.Services;
using Xunit;

namespace Darkness.Tests.Services;

public class WeaponSkillServiceTests
{
    private readonly WeaponSkillService _service = new();

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
}
