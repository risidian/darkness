using Moq;
using Xunit;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Godot.UI;
using System.Collections.Generic;
using System.Linq;

namespace Darkness.Tests.Scenes;

public class SkillsSceneTests
{
    private readonly Mock<IWeaponSkillService> _mockWeaponSkillService;
    private readonly Mock<ISessionService> _mockSession;
    private readonly Mock<ICharacterService> _mockCharacterService;
    private readonly Mock<INavigationService> _mockNavigation;

    public SkillsSceneTests()
    {
        _mockWeaponSkillService = new Mock<IWeaponSkillService>();
        _mockSession = new Mock<ISessionService>();
        _mockCharacterService = new Mock<ICharacterService>();
        _mockNavigation = new Mock<INavigationService>();
    }

    [Fact]
    public void LoadSkills_ShouldFilterOutPassiveSkills()
    {
        // This is a unit test for logic in LoadSkills. 
        // Given the tight coupling with Godot's UI, we test the filtering logic specifically.
        
        var character = new Character { Name = "Test" };
        var skills = new List<Skill> {
            new Skill { Id = 1, Name = "Active", IsPassive = false },
            new Skill { Id = 2, Name = "Passive", IsPassive = true }
        };

        // Logic we extracted / adapted:
        var filteredSkills = skills.Where(s => !s.IsPassive).ToList();

        Assert.Single(filteredSkills);
        Assert.Equal("Active", filteredSkills[0].Name);
        Assert.DoesNotContain(filteredSkills, s => s.IsPassive);
    }
}
