using Xunit;
using Darkness.Core.Logic;
using Darkness.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Darkness.Tests.Scenes;

public class SkillsSceneTests
{
    [Fact]
    public void ActiveOnly_ShouldFilterOutPassiveSkills()
    {
        // SkillsScene populates its available-skills list via SkillFilter.ActiveOnly.
        // This exercises that shared production filter directly.
        var skills = new List<Skill>
        {
            new Skill { Id = 1, Name = "Active", IsPassive = false },
            new Skill { Id = 2, Name = "Passive", IsPassive = true }
        };

        var filteredSkills = SkillFilter.ActiveOnly(skills).ToList();

        Assert.Single(filteredSkills);
        Assert.Equal("Active", filteredSkills[0].Name);
        Assert.DoesNotContain(filteredSkills, s => s.IsPassive);
    }
}
