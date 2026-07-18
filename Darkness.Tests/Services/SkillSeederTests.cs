using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Moq;
using Xunit;
using System.IO;

namespace Darkness.Tests.Services;

public class SkillSeederTests
{
    [Fact]
    public void Seed_ShouldLoadSkillsFromFilesystemToDatabase()
    {
        // Arrange
        var mockFs = new Mock<IFileSystemService>();
        var json = @"[
            { ""Id"": 1, ""Name"": ""Test Skill"", ""Description"": ""Test Desc"", ""WeaponRequirement"": ""Wand"" }
        ]";
        mockFs.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(json);

        var seeder = new SkillSeeder(mockFs.Object);
        using var db = new LiteDatabase(new MemoryStream(), new BsonMapper());

        // Act
        seeder.Seed(db);

        // Assert
        var col = db.GetCollection<Skill>("skills");
        var skill = col.FindOne(s => s.Id == 1);
        Assert.NotNull(skill);
        Assert.Equal("Test Skill", skill.Name);
        Assert.Equal("Wand", skill.WeaponRequirement);
    }

    [Fact]
    public void Seed_ShouldParseStringEnumActionType_AndPopulateSkills()
    {
        // Regression: skills.json uses string enum values for AssociatedAction
        // (e.g. "Shoot"). Without a JsonStringEnumConverter the deserialize throws,
        // the catch swallows it, and the skills collection stays empty.
        var mockFs = new Mock<IFileSystemService>();
        var json = @"[
            { ""Id"": 1, ""Name"": ""Arcane Bolt"", ""Description"": ""A bolt"", ""AssociatedAction"": ""Shoot"", ""WeaponRequirement"": ""Wand"" }
        ]";
        mockFs.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(json);

        var seeder = new SkillSeeder(mockFs.Object);
        using var db = new LiteDatabase(new MemoryStream(), new BsonMapper());

        seeder.Seed(db);

        var col = db.GetCollection<Skill>("skills");
        Assert.True(col.Count() > 0, "skills collection should be non-empty after seeding");
        var skill = col.FindOne(s => s.Id == 1);
        Assert.NotNull(skill);
        Assert.Equal(ActionType.Shoot, skill.AssociatedAction);
    }
}
