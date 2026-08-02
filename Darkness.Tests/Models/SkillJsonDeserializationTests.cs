using System.Text.Json;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models
{
    public class SkillJsonDeserializationTests
    {
        [Fact]
        public void Skill_Deserializes_ClassRequirement()
        {
            var json = @"{ ""Id"": 1, ""Name"": ""Arcane Bolt"", ""ClassRequirement"": ""Mage"" }";
            var skill = JsonSerializer.Deserialize<Skill>(json);
            
            Assert.NotNull(skill);
            Assert.Equal("Mage", skill.ClassRequirement);
        }
    }
}
